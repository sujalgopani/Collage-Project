using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ExamNest.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AccessController : ControllerBase
    {
        [HttpGet("me")]
        public IActionResult Me()
        {
            var roles = User.FindAll(ClaimTypes.Role)
                            .Select(r => r.Value);

            return Ok(new
            {
                Message = "Authenticated user",
                Username = User.Identity?.Name,
                Roles = roles
            });
        }

        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        public IActionResult Admin()
        {
            return Ok(new { Message = "Admin access granted." });
        }

        [HttpGet("teacher")]
        [Authorize(Policy = "TeacherOnly")]
        public IActionResult Teacher()
        {
            return Ok(new { Message = "Teacher access granted." });
        }

        [HttpGet("student")]
        [Authorize(Policy = "StudentOnly")]
        public IActionResult Student()
        {
            return Ok(new { Message = "Student access granted." });
        }
    }
}
