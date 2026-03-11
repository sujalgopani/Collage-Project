using ExamNest.Data;
using ExamNest.Models;
using ExamNest.Models.DTOs.Student;
using ExamNest.Models.Payment;
using ExamNest.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ExamNest.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "StudentOnly")]
    public class StudentController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly Student _studentservice;

        public StudentController(AppDbContext context, Student studentservice)
        {
            _context = context;
            _studentservice = studentservice;
        }

        [HttpGet("published-courses")]
        public async Task<IActionResult> GetPublishedCourses()
        {
            var courses = await _context.Courses
                .Where(c => c.IsPublished)
                .Select(c => new CourseCardStudentDTO
                {
                    CourseId = c.CourseId,
                    Title = c.Title,
                    ThumbnailUrl = c.ThumbailUrl,
                    Fees = c.Fees,
                    StartDate = c.StartDate,
                    EndDate = c.EndDate
                })
                .ToListAsync();

            return Ok(courses);
        }

        [HttpGet("Get-Subscribed-course")]
        public async Task<IActionResult> GetCourses()
        {
            if (!TryGetStudentId(out var studentId))
                return Unauthorized("Invalid student token.");

            var courses = await _studentservice.GetCourses(studentId);
            return Ok(courses);
        }

        [HttpGet("course/{courseId}/videos")]
        public async Task<IActionResult> GetCourseVideos(int courseId)
        {
            var subscription = await GetActiveSubscriptionForCurrentStudent(courseId);
            if (subscription == null)
                return Unauthorized("Only subscribed students can access this course content.");

            var course = await _context.Courses.FindAsync(courseId);
            if (course == null)
                return NotFound("Course not found.");

            if (DateTime.Now < course.StartDate)
                return BadRequest("Course not started yet");

            if (DateTime.Now > course.EndDate)
                return BadRequest("Course ended");

            var videos = await _context.CourseMedias
                .Where(v => v.CourseId == courseId)
                .ToListAsync();

            return Ok(videos);
        }

        [HttpGet("course/{courseId}/access")]
        public async Task<IActionResult> CheckCourseAccess(int courseId)
        {
            var subscription = await GetActiveSubscriptionForCurrentStudent(courseId);

            if (subscription == null)
            {
                return Unauthorized(new
                {
                    hasAccess = false,
                    message = "Subscription required for this course."
                });
            }

            return Ok(new
            {
                hasAccess = true,
                message = "Subscribed student can access exams, videos, and all course resources."
            });
        }

		[HttpGet("GetCourseById")]
		public async Task<IActionResult> GetCourseById(int courseId)
		{
			var data = await _studentservice.GetCourseById(courseId);
			if (data == null)
			{
				return NotFound(new { message = "Course not found" });
			}
			return Ok(data);
		}

		private async Task<Subscription?> GetActiveSubscriptionForCurrentStudent(int courseId)
        {
            if (!TryGetStudentId(out var studentId))
                return null;

            return await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.StudentId == studentId
                    && s.CourseId == courseId
                    && s.Status == "Active");
        }

        private bool TryGetStudentId(out int studentId)
        {
            studentId = 0;
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(id, out studentId);
        }
    }
}
