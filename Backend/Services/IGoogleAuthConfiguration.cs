namespace ExamNest.Services
{
    public interface IGoogleAuthConfiguration
    {
        string? GetFrontendClientId();
        string[] GetAllowedAudiences();
    }
}
