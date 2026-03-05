using Google.Apis.Auth;

namespace ExamNest.Services
{
    public class GoogleTokenValidator : IGoogleTokenValidator
    {
        private readonly IConfiguration _configuration;

        public GoogleTokenValidator(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<GoogleUserInfo?> ValidateAsync(string idToken)
        {
            var audience = _configuration["GoogleAuth:ClientId"];
            if (string.IsNullOrWhiteSpace(audience))
            {
                throw new InvalidOperationException("GoogleAuth:ClientId is missing.");
            }

            try
            {
                var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { audience }
                });

                return new GoogleUserInfo(
                    payload.Subject,
                    payload.Email,
                    payload.GivenName ?? string.Empty,
                    payload.FamilyName ?? string.Empty,
                    payload.Picture
                );
            }
            catch
            {
                return null;
            }
        }
    }
}
