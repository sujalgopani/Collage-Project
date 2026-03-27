namespace ExamNest.Services
{
    public class GoogleAuthConfiguration : IGoogleAuthConfiguration
    {
        private readonly IConfiguration _configuration;
        private readonly IHostEnvironment _environment;

        public GoogleAuthConfiguration(IConfiguration configuration, IHostEnvironment environment)
        {
            _configuration = configuration;
            _environment = environment;
        }

        public string? GetFrontendClientId()
        {
            return GetCandidates().FirstOrDefault();
        }

        public string[] GetAllowedAudiences()
        {
            return GetCandidates().ToArray();
        }

        private IEnumerable<string> GetCandidates()
        {
            var configuredClientIds = _configuration.GetSection("GoogleAuth:AllowedClientIds").Get<string[]>() ?? Array.Empty<string>();
            var candidates = configuredClientIds
                .Append(_configuration["GoogleAuth:FrontendClientId"])
                .Append(_configuration["GoogleAuth:ClientId"])
                .Append(_configuration["Authentication:Google:ClientId"])
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id!.Trim())
                .Where(id => !IsPlaceholder(id))
                .Distinct(StringComparer.Ordinal);

            if (_environment.IsDevelopment())
            {
                return candidates;
            }

            // In non-development, only allow Google Web client IDs.
            return candidates.Where(id => id.EndsWith(".apps.googleusercontent.com", StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsPlaceholder(string value)
        {
            return value.Contains("YOUR_GOOGLE_CLIENT_ID", StringComparison.OrdinalIgnoreCase)
                || value.Equals("CHANGE_ME", StringComparison.OrdinalIgnoreCase);
        }
    }
}
