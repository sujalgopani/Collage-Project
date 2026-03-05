namespace ExamNest.Services
{
    public record GoogleUserInfo(string Subject, string Email, string GivenName, string FamilyName, string? Picture);

    public interface IGoogleTokenValidator
    {
        Task<GoogleUserInfo?> ValidateAsync(string idToken);
    }
}
