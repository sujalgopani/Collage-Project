using System.Security.Claims;
using ExamNest.Models;
using ExamNest.Models.DTOs.Exam;
using ExamNest.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Query;

namespace ExamNest.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize(Roles = "Teacher")]
    public class ExamController : ControllerBase
    {
        private readonly ExamService _examservice;
        public ExamController(ExamService examservice)
        {
            _examservice = examservice;
        }


		[HttpGet]
		public async Task<IActionResult> GetTeacherWiseCourse()
		{
			var teacherIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (teacherIdClaim == null)
				return Unauthorized("Teacher ID not found in token");

			int teacherId = int.Parse(teacherIdClaim);

			var exams = await _examservice.GetTeacherWiseCourse(teacherId);

			if (exams == null || exams.Count == 0)
				return NotFound("No Exams Found!");

			return Ok(exams);
		}

	}
}
