using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ExamNest.Data;
using ExamNest.Models;
using ExamNest.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace ExamNest.Services
{
    public class AuthService : IAuthService
    {
        private const int MaxFailedAttempts = 5;
        private const int OtpExpiryMinutes = 10;

        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IEmailSender _emailSender;
        private readonly IGoogleTokenValidator _googleTokenValidator;

        public AuthService(
            AppDbContext context,
            IConfiguration configuration,
            IEmailSender emailSender,
            IGoogleTokenValidator googleTokenValidator)
        {
            _context = context;
            _configuration = configuration;
            _emailSender = emailSender;
            _googleTokenValidator = googleTokenValidator;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request)
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            //var normalizedUsername = request.Username.Trim().ToLowerInvariant();

            //if (await _context.Users.AnyAsync(u => u.Email.ToLower() == normalizedEmail))
            //{
            //    return new AuthResponseDto { Success = false, Message = "Email already Taken." };
            //}

            //if (await _context.Users.AnyAsync(u => u.Username.ToLower() == normalizedUsername))
            //{
            //    return new AuthResponseDto { Success = false, Message = "Username already exists." };
            //}

            var studentRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Student");
            if (studentRole == null)
            {
                return new AuthResponseDto { Success = false, Message = "Student role not found." };
            }

            // fname + lname lowercase
            var baseName = (request.FirstName + request.LastName).ToLower();

            // last 4 digits of mobile
            var last4 = request.Phone!.Substring(request.Phone.Length - 4);

            // base username
            var username = baseName + last4;

            Random rnd = new Random();

            // loop until unique username found
            while (await _context.Users.AnyAsync(u => u.Username == username))
            {
                var num = rnd.Next(10, 100); // 2 digit
                username = baseName + last4 + num;
            }


            var user = new User
            {
                FirstName = request.FirstName.Trim(),
                MiddleName = string.IsNullOrWhiteSpace(request.MiddleName) ? null : request.MiddleName.Trim(),
                LastName = request.LastName.Trim(),
                Email = normalizedEmail,
                Username = username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
                RoleId = request.Role !=0 ? request.Role : studentRole.RoleId,
                IsActive = false,
                FailedLoginAttempts = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var otp = await CreateAndStoreOtpAsync(user.UserId);
            await SendOtpEmailAsync(user.Email, otp);

            return new AuthResponseDto
            {
                Success = true,
                Message = "Registration successful. Verify your email with OTP."
            };
        }

        public async Task<AuthResponseDto> VerifyEmailOtpAsync(VerifyEmailOtpRequestDto request)
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

            if (user == null)
            {
                return new AuthResponseDto { Success = false, Message = "User not found." };
            }

            var otpRecord = await _context.EmailOtps
                .Where(o => o.UserId == user.UserId && !o.IsUsed)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (otpRecord == null || otpRecord.ExpiresAt < DateTime.UtcNow)
            {
                return new AuthResponseDto { Success = false, Message = "OTP expired. Request a new OTP." };
            }

            if (!BCrypt.Net.BCrypt.Verify(request.Otp, otpRecord.OtpHash))
            {
                return new AuthResponseDto { Success = false, Message = "Invalid OTP." };
            }

            otpRecord.IsUsed = true;
            user.IsActive = true;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new AuthResponseDto { Success = true, Message = "Email verified successfully." };
        }

        public async Task<AuthResponseDto> ResendEmailOtpAsync(ResendEmailOtpRequestDto request)
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

            if (user == null)
            {
                return new AuthResponseDto { Success = false, Message = "User not found." };
            }

            if (user.IsActive)
            {
                return new AuthResponseDto { Success = false, Message = "Email is already verified." };
            }

            var otp = await CreateAndStoreOtpAsync(user.UserId);
            await SendOtpEmailAsync(user.Email, otp);

            return new AuthResponseDto { Success = true, Message = "OTP sent successfully." };
        }

        public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
        {
            var identifier = request.EmailOrUsername.Trim().ToLowerInvariant();
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u =>
                    u.Email.ToLower() == identifier ||
                    u.Username.ToLower() == identifier);

            if (user == null)
            {
                return new AuthResponseDto { Success = false, Message = "Invalid credentials." };
            }

            if (!user.IsActive)
            {
                return new AuthResponseDto { Success = false, Message = "Email not verified. Verify OTP first." };
            }

            if (string.IsNullOrWhiteSpace(user.PasswordHash) ||
                !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                user.FailedLoginAttempts += 1;
                if (user.FailedLoginAttempts >= MaxFailedAttempts)
                {
                    user.IsActive = false;
                }

                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return new AuthResponseDto { Success = false, Message = "Invalid credentials." };
            }

            user.FailedLoginAttempts = 0;
            user.LastLoginAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(user);
            return BuildSuccessLoginResponse(user, token, "Login successful.");
        }

        public async Task<AuthResponseDto> GoogleLoginAsync(GoogleLoginRequestDto request)
        {
            var googleUser = await _googleTokenValidator.ValidateAsync(request.IdToken);
            if (googleUser == null || string.IsNullOrWhiteSpace(googleUser.Email))
            {
                return new AuthResponseDto { Success = false, Message = "Invalid Google token." };
            }

            var normalizedEmail = googleUser.Email.Trim().ToLowerInvariant();
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

            if (user == null)
            {
                var studentRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Student");
                if (studentRole == null)
                {
                    return new AuthResponseDto { Success = false, Message = "Student role not found." };
                }

                var baseUsername = BuildBaseUsername(normalizedEmail, googleUser.GivenName, googleUser.FamilyName);
                var uniqueUsername = await GenerateUniqueUsernameAsync(baseUsername);

                user = new User
                {
                    FirstName = string.IsNullOrWhiteSpace(googleUser.GivenName) ? "Google" : googleUser.GivenName.Trim(),
                    LastName = string.IsNullOrWhiteSpace(googleUser.FamilyName) ? "User" : googleUser.FamilyName.Trim(),
                    Email = normalizedEmail,
                    Username = uniqueUsername,
                    PasswordHash = null,
                    Phone = null,
                    RoleId = studentRole.RoleId,
                    IsActive = true,
                    FailedLoginAttempts = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                user = await _context.Users
                    .Include(u => u.Role)
                    .FirstAsync(u => u.UserId == user.UserId);
            }
            else
            {
                if (!user.IsActive)
                {
                    user.IsActive = true;
                }
            }

            var existingGoogleAuth = await _context.UserGoogleAuths
                .FirstOrDefaultAsync(g => g.UserId == user.UserId);

            if (existingGoogleAuth == null)
            {
                _context.UserGoogleAuths.Add(new UserGoogleAuth
                {
                    UserId = user.UserId,
                    GoogleSub = googleUser.Subject,
                    GoogleEmail = normalizedEmail,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            else
            {
                existingGoogleAuth.GoogleSub = googleUser.Subject;
                existingGoogleAuth.GoogleEmail = normalizedEmail;
                existingGoogleAuth.UpdatedAt = DateTime.UtcNow;
            }

            user.LastLoginAt = DateTime.UtcNow;
            user.FailedLoginAttempts = 0;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(user);
            return BuildSuccessLoginResponse(user, token, "Google login successful.");
        }

        private async Task<string> CreateAndStoreOtpAsync(int userId)
        {
            var otp = Random.Shared.Next(100000, 999999).ToString();
            var otpHash = BCrypt.Net.BCrypt.HashPassword(otp);

            var otpEntity = new EmailOtp
            {
                UserId = userId,
                OtpHash = otpHash,
                ExpiresAt = DateTime.UtcNow.AddMinutes(OtpExpiryMinutes),
                IsUsed = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.EmailOtps.Add(otpEntity);
            await _context.SaveChangesAsync();

            return otp;
        }

        private async Task SendOtpEmailAsync(string email, string otp)
        {
            var subject = "ExamNest Email Verification OTP";
            var body = $"Your OTP is {otp}. It expires in {OtpExpiryMinutes} minutes.";
            await _emailSender.SendEmailAsync(email, subject, body);
        }

        private async Task<string> GenerateUniqueUsernameAsync(string baseUsername)
        {
            var candidate = baseUsername;
            var i = 1;

            while (await _context.Users.AnyAsync(u => u.Username.ToLower() == candidate.ToLower()))
            {
                candidate = $"{baseUsername}{i}";
                i++;
            }

            return candidate;
        }

        private static string BuildBaseUsername(string email, string firstName, string lastName)
        {
            var fromNames = $"{firstName}{lastName}".Trim().Replace(" ", string.Empty);
            if (!string.IsNullOrWhiteSpace(fromNames))
            {
                return fromNames.ToLowerInvariant();
            }

            return email.Split('@')[0].ToLowerInvariant();
        }

        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");

            var keyString = jwtSettings["Key"];
            var issuer = jwtSettings["Issuer"];
            var audience = jwtSettings["Audience"];
            var durationString = jwtSettings["DurationInMinutes"];

            if (string.IsNullOrWhiteSpace(keyString) ||
                string.IsNullOrWhiteSpace(issuer) ||
                string.IsNullOrWhiteSpace(audience) ||
                string.IsNullOrWhiteSpace(durationString))
            {
                throw new Exception("JWT configuration is missing or invalid.");
            }

            if (!double.TryParse(durationString, out var durationMinutes))
            {
                throw new Exception("Invalid JWT duration configuration.");
            }

            var key = Encoding.UTF8.GetBytes(keyString);

            var roleName = user.Role?.RoleName ?? "Student";
            var fullName = $"{user.FirstName} {user.LastName}".Trim();

            var claims = new List<Claim>
                {
                    new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
                    new Claim(JwtRegisteredClaimNames.Email, user.Email),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, roleName),
                    new Claim("full_name", fullName),
                    new Claim("role_id", user.Role?.RoleId.ToString() ?? "0"),
                };

            var credentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(durationMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static AuthResponseDto BuildSuccessLoginResponse(User user, string token, string message)
        {
            var fullName = string.Join(" ",
                new[] { user.FirstName, user.MiddleName, user.LastName }
                .Where(s => !string.IsNullOrWhiteSpace(s)));

            return new AuthResponseDto
            {
                Success = true,
                Message = message,
                Token = token,
                FullName = fullName,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role?.RoleName
            };
        }
    }
}
