using Google.Apis.Auth;

namespace ExamNest.Services
{
    public class GoogleTokenValidator : IGoogleTokenValidator
    {
        private readonly IGoogleAuthConfiguration _googleAuthConfiguration;
        private readonly ILogger<GoogleTokenValidator> _logger;

        public GoogleTokenValidator(
            IGoogleAuthConfiguration googleAuthConfiguration,
            ILogger<GoogleTokenValidator> logger)
        {
            _googleAuthConfiguration = googleAuthConfiguration;
            _logger = logger;
        }

        public async Task<GoogleTokenValidationResult> ValidateAsync(string idToken)
        {
            var audiences = ResolveAllowedAudiences();
            if (audiences.Length == 0)
            {
                throw new InvalidOperationException(
                    "Google OAuth client id is missing. Configure GoogleAuth:FrontendClientId, GoogleAuth:ClientId, Authentication:Google:ClientId, or GoogleAuth:AllowedClientIds.");
            }

            try
            {
                var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = audiences
                });

                return new GoogleTokenValidationResult(
                    new GoogleUserInfo(
                        payload.Subject,
                        payload.Email,
                        payload.GivenName ?? string.Empty,
                        payload.FamilyName ?? string.Empty,
                        payload.Picture
                    ),
                    null,
                    null
                );
            }
            catch (InvalidJwtException ex) when (ex.Message.Contains("aud", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(ex, "Google token rejected due to invalid audience.");
                return new GoogleTokenValidationResult(null, "invalid_audience", "Google token audience mismatch. Verify frontend and backend use the same Google Client ID.");
            }
            catch (InvalidJwtException ex)
            {
                _logger.LogWarning(ex, "Google token rejected during validation.");
                return new GoogleTokenValidationResult(null, "unauthorized_client", "Google token validation failed. Please sign in again.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected Google token validation error.");
                return new GoogleTokenValidationResult(null, "access_blocked", "Google Sign-In is currently blocked. Confirm OAuth consent screen settings and authorized users.");
            }
        }

        private string[] ResolveAllowedAudiences()
        {
            return _googleAuthConfiguration.GetAllowedAudiences();
        }
    }
}
