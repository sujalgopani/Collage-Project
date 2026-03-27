namespace ExamNest.Services
{
    public record GoogleUserInfo(string Subject, string Email, string GivenName, string FamilyName, string? Picture);

    public record GoogleTokenValidationResult(GoogleUserInfo? User, string? ErrorCode, string? ErrorMessage)
    {
        public bool IsSuccess => User is not null;
    }

    public interface IGoogleTokenValidator
    {
        Task<GoogleTokenValidationResult> ValidateAsync(string idToken);
    }
}
