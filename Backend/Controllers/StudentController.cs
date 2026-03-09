using ExamNest.Data;
using ExamNest.Models.DTOs.Student;
using ExamNest.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ExamNest.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudentController : ControllerBase
    {

        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly Student _studentservice;


        public StudentController(AppDbContext context, IWebHostEnvironment env, Student studentservice)
        {
            _context = context;
            _env = env;
            _studentservice = studentservice;
        }


        [HttpGet("published-courses")]
        public async Task<IActionResult> GetPublishedCourses()
        {
            var courses = await _context.Courses
                .Where(c => c.IsPublished == true)
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
            var studentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var courses = await _studentservice.GetCourses(studentId);
            return Ok(courses);
        }


        [HttpGet("course/{courseId}/videos")]
        public async Task<IActionResult> GetCourseVideos(int courseId)
        {
            var studentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var subscription = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.StudentId == studentId && s.CourseId == courseId);

            if (subscription == null)
                return Unauthorized("You are not subscribed");

            var course = await _context.Courses.FindAsync(courseId);

            if (DateTime.Now < course!.StartDate)
                return BadRequest("Course not started yet");

            if (DateTime.Now > course.EndDate)
                return BadRequest("Course ended");

            var videos = await _context.CourseMedias
                .Where(v => v.CourseId == courseId)
                .ToListAsync();

            return Ok(videos);
        }


    }
}
