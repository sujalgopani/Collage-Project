using ExamNest.Models.DTOs;

namespace ExamNest.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request);
        Task<AuthResponseDto> LoginAsync(LoginRequestDto request);
        Task<AuthResponseDto> VerifyEmailOtpAsync(VerifyEmailOtpRequestDto request);
        Task<AuthResponseDto> ResendEmailOtpAsync(ResendEmailOtpRequestDto request);
        Task<AuthResponseDto> GoogleLoginAsync(GoogleLoginRequestDto request);
        Task<AuthResponseDto> ForgotPasswordAsync(ForgotPasswordRequestDto request);
        Task<AuthResponseDto> VerifyForgotPasswordOtpAsync(VerifyForgotPasswordOtpRequestDto request);
        Task<AuthResponseDto> ResetPasswordAsync(ResetPasswordRequestDto request);
    }
}
