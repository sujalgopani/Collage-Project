using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
            return Ok(new
            {
                Message = "Authenticated user",
                Username = User.Identity?.Name,
                Roles = User.Claims
                    .Where(c => c.Type.EndsWith("/role") || c.Type == "role")
                    .Select(c => c.Value)
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
