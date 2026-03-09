using ExamNest.Data;
using ExamNest.Models;
using ExamNest.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using System.Security.Claims;

namespace ExamNest.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class TeacherController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public TeacherController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }


        [HttpPost("create")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(long.MaxValue)]
        [DisableRequestSizeLimit]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> CreateCourse([FromForm] CourseCreateDTO dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            string thumbnailUrl = "";

            // Thumbnail Save
            if (dto.ThumbailUrl != null)
            {
                var rootPath = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
                var folder = Path.Combine(rootPath, "CourseThumbnail");

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                var fileName = Guid.NewGuid() + Path.GetExtension(dto.ThumbailUrl!.FileName);
                var filePath = Path.Combine(folder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await dto.ThumbailUrl.CopyToAsync(stream);
                }

                thumbnailUrl += "/CourseThumbnail/" + fileName;
            }

            var course = new Course
            {
                Title = dto.Title,
                Description = dto.Description,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                TeacherId = userId,
                Fees = dto.Fees,
                ThumbailUrl = thumbnailUrl,
                CreatedAt = DateTime.Now
            };

            _context.Courses.Add(course);
            await _context.SaveChangesAsync();
            thumbnailUrl = "";
            // 🎥 Multiple Videos Save
            if (dto.Files != null && dto.Files.Count > 0)
            {
                var rootPath = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
                var videoFolder = Path.Combine(rootPath, "CourseVideos");

                if (!Directory.Exists(videoFolder))
                    Directory.CreateDirectory(videoFolder);

                foreach (var file in dto.Files)
                {
                    var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                    var filePath = Path.Combine(videoFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    var media = new CourseMedia
                    {
                        CourseId = course.CourseId,
                        FilePath = "/CourseVideos/" + fileName
                    };

                    _context.CourseMedias.Add(media);
                }

                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Course Created Successfully" });
        }


        [HttpGet("mycourses")]
        public async Task<IActionResult> GetMyCourses()
        {
            var teacherId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var courses = await _context.Courses
                .Where(c => c.TeacherId == teacherId)
                .Include(c => c.CourseMedias)
                .Select(c => new
                {
                    c.CourseId,
                    c.Title,
                    c.Description,
                    c.StartDate,
                    c.EndDate,
                    c.Fees,
                    c.ThumbailUrl,
                    c.IsPublished,
                    Videos = c.CourseMedias!.Select(m => new
                    {
                        m.CourseMediaId,
                        m.FilePath
                    })
                })
                .ToListAsync();

            return Ok(courses);
        }


        
    }
}

