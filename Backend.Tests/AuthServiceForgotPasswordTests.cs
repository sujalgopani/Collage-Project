using ExamNest.Data;
using ExamNest.Models;
using ExamNest.Models.DTOs;
using ExamNest.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.RegularExpressions;
using Xunit;

namespace Backend.Tests;

public class AuthServiceForgotPasswordTests
{
    [Fact]
    public async Task ForgotPassword_To_VerifyOtp_To_ResetPassword_Works_EndToEnd()
    {
        using var context = BuildContext();
        await SeedUserAsync(context, "student@example.com", "OldPassword123!");

        var emailSender = new FakeEmailSender();
        var service = BuildService(context, emailSender);

        var forgot = await service.ForgotPasswordAsync(new ForgotPasswordRequestDto { Email = "student@example.com" });
        Assert.True(forgot.Success);

        var otp = context.EmailOtps.OrderByDescending(x => x.CreatedAt).First();
        var verifyWrong = await service.VerifyForgotPasswordOtpAsync(new VerifyForgotPasswordOtpRequestDto
        {
            Email = "student@example.com",
            Otp = "000000"
        });
        Assert.False(verifyWrong.Success);

        var verify = new AuthResponseDto { Success = false, Message = "No OTP candidate could be verified." };
        foreach (var candidate in ExtractOtpCandidatesFromLatestEmail(emailSender))
        {
            verify = await service.VerifyForgotPasswordOtpAsync(new VerifyForgotPasswordOtpRequestDto
            {
                Email = "student@example.com",
                Otp = candidate
            });

            if (verify.Success)
            {
                break;
            }
        }

        Assert.True(verify.Success, verify.Message);

        var reset = await service.ResetPasswordAsync(new ResetPasswordRequestDto
        {
            Email = "student@example.com",
            NewPassword = "NewPassword123!"
        });
        Assert.True(reset.Success);

        var loginOld = await service.LoginAsync(new LoginRequestDto
        {
            EmailOrUsername = "student@example.com",
            Password = "OldPassword123!"
        });
        Assert.False(loginOld.Success);

        var loginNew = await service.LoginAsync(new LoginRequestDto
        {
            EmailOrUsername = "student@example.com",
            Password = "NewPassword123!"
        });
        Assert.True(loginNew.Success);
    }

    [Fact]
    public async Task ResetPassword_WithoutOtpVerification_Fails()
    {
        using var context = BuildContext();
        await SeedUserAsync(context, "student2@example.com", "OldPassword123!");

        var service = BuildService(context, new FakeEmailSender());

        var reset = await service.ResetPasswordAsync(new ResetPasswordRequestDto
        {
            Email = "student2@example.com",
            NewPassword = "NewPassword123!"
        });

        Assert.False(reset.Success);
        Assert.Contains("Verify forgot password OTP", reset.Message);
    }

    private static AuthService BuildService(AppDbContext context, FakeEmailSender emailSender)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:Key"] = "super-secret-key-for-tests-only-1234567890",
                ["JwtSettings:Issuer"] = "test-issuer",
                ["JwtSettings:Audience"] = "test-audience",
                ["JwtSettings:DurationInMinutes"] = "60"
            })
            .Build();

        return new AuthService(
            context,
            config,
            emailSender,
            new FakeGoogleTokenValidator(),
            NullLogger<AuthService>.Instance);
    }

    private static AppDbContext BuildContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static async Task SeedUserAsync(AppDbContext context, string email, string password)
    {
        context.Roles.Add(new Role
        {
            RoleId = 3,
            RoleName = "Student",
            CreatedAt = DateTime.UtcNow
        });

        context.Users.Add(new User
        {
            FirstName = "Test",
            LastName = "User",
            Email = email,
            Username = email.Split('@')[0],
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            RoleId = 3,
            IsActive = true,
            FailedLoginAttempts = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();
    }

    private static IEnumerable<string> ExtractOtpCandidatesFromLatestEmail(FakeEmailSender sender)
    {
        var body = sender.SentEmails.Last().Body;
        var matches = Regex.Matches(body, @"\b\d{6}\b")
            .Select(m => m.Value)
            .Distinct()
            .ToList();

        Assert.NotEmpty(matches);
        return matches;
    }

    private sealed class FakeEmailSender : IEmailSender
    {
        public List<(string To, string Subject, string Body)> SentEmails { get; } = [];

        public Task SendEmailAsync(string toEmail, string subject, string body, bool isBodyHtml = false)
        {
            SentEmails.Add((toEmail, subject, body));
            return Task.CompletedTask;
        }
    }

    private sealed class FakeGoogleTokenValidator : IGoogleTokenValidator
    {
        public Task<GoogleTokenValidationResult> ValidateAsync(string idToken)
            => Task.FromResult(new GoogleTokenValidationResult(null, "not_used", "not_used"));
    }
}
