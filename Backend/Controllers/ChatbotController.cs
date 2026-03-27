using System.Security.Claims;
using ExamNest.Models.DTOs.Chatbot;
using ExamNest.Services.Chatbot;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamNest.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Teacher,Admin,Student")]
    public class ChatbotController : ControllerBase
    {
        private readonly IAppChatbotService _chatbotService;
        private readonly ILogger<ChatbotController> _logger;

        public ChatbotController(
            IAppChatbotService chatbotService,
            ILogger<ChatbotController> logger)
        {
            _chatbotService = chatbotService;
            _logger = logger;
        }

        [HttpPost("ask")]
        public async Task<IActionResult> Ask([FromBody] ChatbotAskRequest request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized("Invalid user token.");
            }

            var role = User.FindFirstValue(ClaimTypes.Role)
                       ?? User.FindFirstValue("role")
                       ?? User.Claims.FirstOrDefault(c =>
                           c.Type.Equals("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", StringComparison.OrdinalIgnoreCase))
                           ?.Value
                       ?? "Student";

            try
            {
                var response = await _chatbotService.AskAsync(userId, role, request, cancellationToken);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Chatbot ask failed for user {UserId} with role {Role}.", userId, role);

                return Ok(new ChatbotAskResponse
                {
                    Role = NormalizeRole(role),
                    Answer = "Assistant is temporarily busy. Please try again in a moment.",
                    UsedFallback = true,
                    GeneratedAtUtc = DateTime.UtcNow
                });
            }
        }

        private bool TryGetCurrentUserId(out int userId)
        {
            userId = 0;
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            return int.TryParse(claim, out userId);
        }

        private static string NormalizeRole(string? role)
        {
            if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                return "Admin";
            }

            if (string.Equals(role, "Teacher", StringComparison.OrdinalIgnoreCase))
            {
                return "Teacher";
            }

            return "Student";
        }
    }
}
