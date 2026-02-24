using ExamNest.Models.DTOs;
using ExamNest.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExamNest.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.RegisterAsync(request);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.LoginAsync(request);

            if (!result.Success)
                return Unauthorized(result);

            return Ok(result);
        }

        [HttpPost("verify-email-otp")]
        public async Task<IActionResult> VerifyEmailOtp([FromBody] VerifyEmailOtpRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.VerifyEmailOtpAsync(request);
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("resend-email-otp")]
        public async Task<IActionResult> ResendEmailOtp([FromBody] ResendEmailOtpRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.ResendEmailOtpAsync(request);
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.GoogleLoginAsync(request);
            if (!result.Success)
                return Unauthorized(result);

            return Ok(result);
        }
    }
}
