using System.Security.Claims;
using ExamNest.Data;
using ExamNest.Models;
using ExamNest.Models.DTOs.Exam;
using ExamNest.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamNest.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize(Roles = "Teacher,Admin")]
    public class ExamController : ControllerBase
    {
        private readonly ExamService _examservice;
        private readonly AppDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly IConfiguration _configuration;
        private readonly AppActivityEmailService _activityEmailService;

        public ExamController(
            ExamService examservice,
            AppDbContext context,
            IEmailSender emailSender,
            IConfiguration configuration,
            AppActivityEmailService activityEmailService)
        {
            _examservice = examservice;
            _context = context;
            _emailSender = emailSender;
            _configuration = configuration;
            _activityEmailService = activityEmailService;
        }

        [HttpGet]
        public async Task<IActionResult> GetTeacherWiseCourse()
        {
            var teacherIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (teacherIdClaim == null)
                return Unauthorized("Teacher ID not found in token");

            var teacherId = int.Parse(teacherIdClaim);

            var exams = await _examservice.GetTeacherWiseCourse(teacherId);

            if (exams == null || exams.Count == 0)
                return NotFound("No Exams Found!");

            return Ok(exams);
        }

        [HttpPut("publish-result/{examId}")]
        public async Task<IActionResult> PublishResult(int examId)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
                return Unauthorized("Invalid user token.");

            var examMeta = await _context.Exams
                .AsNoTracking()
                .Select(e => new
                {
                    e.ExamId,
                    e.TeacherId,
                    e.Title,
                    CourseTitle = e.Course != null ? e.Course.Title : "Course"
                })
                .FirstOrDefaultAsync(e => e.ExamId == examId);

            if (examMeta == null)
                return NotFound("Exam not found.");

            var isAdmin = IsCurrentUserAdmin();
            if (!isAdmin && examMeta.TeacherId != currentUserId)
                return Forbid();

            var result = await _examservice.PublishExamResult(examId);
            if (result == PublishResultOutcome.ExamNotFound)
            {
                return NotFound("Exam not found.");
            }

            if (result == PublishResultOutcome.NoSubmittedAttempts)
            {
                return BadRequest("No submitted attempts found for this exam yet.");
            }

            if (result == PublishResultOutcome.AlreadyPublished)
            {
                return Ok(new
                {
                    message = "Result already published.",
                    examId,
                    published = true
                });
            }

            var notifiedCount = await NotifyStudentsAboutPublishedResultAsync(examId);

            try
            {
                await _activityEmailService.NotifyAdminsAsync(
                    subject: $"Activity: Result Published - {examMeta.Title}",
                    activityTitle: "Exam Result Published",
                    summary: "Results were published for an exam.",
                    detailsRows: new Dictionary<string, string>
                    {
                        ["Exam"] = examMeta.Title,
                        ["Course"] = examMeta.CourseTitle,
                        ["Students Notified"] = notifiedCount.ToString()
                    },
                    actionPathOrUrl: "/admin-dashboard/exams-manage");
            }
            catch
            {
                // Keep publish successful even if admin activity notification fails.
            }

            return Ok(new
            {
                message = "Result published successfully.",
                examId,
                published = true,
                studentsNotified = notifiedCount
            });
        }

        private async Task<int> NotifyStudentsAboutPublishedResultAsync(int examId)
        {
            var exam = await _context.Exams
                .AsNoTracking()
                .Include(e => e.Course)
                .FirstOrDefaultAsync(e => e.ExamId == examId);

            if (exam?.Course == null)
                return 0;

            var attempts = await _context.ExamAttempts
                .AsNoTracking()
                .Include(a => a.Student)
                .Where(a => a.ExamId == examId && a.SubmittedAt != null)
                .ToListAsync();

            if (attempts.Count == 0)
                return 0;

            var latestAttemptsByStudent = attempts
                .Where(a => a.Student != null && !string.IsNullOrWhiteSpace(a.Student.Email))
                .GroupBy(a => a.StudentId)
                .Select(g => g.OrderByDescending(a => a.SubmittedAt ?? a.StartedAt).First())
                .ToList();

            var resultUrl = $"{GetFrontendBaseUrl()}/student-dashboard/student-exam-result";
            var notifiedCount = 0;

            foreach (var attempt in latestAttemptsByStudent)
            {
                if (attempt.Student == null || string.IsNullOrWhiteSpace(attempt.Student.Email))
                    continue;

                var body = EmailTemplateBuilder.BuildExamResultPublishedEmail(
                    GetDisplayName(attempt.Student),
                    exam.Title,
                    exam.Course.Title,
                    attempt.TotalScore,
                    attempt.MaxScore,
                    resultUrl);

                try
                {
                    await _emailSender.SendEmailAsync(
                        attempt.Student.Email,
                        $"Result Published: {exam.Title}",
                        body,
                        isBodyHtml: true);
                    notifiedCount++;
                }
                catch
                {
                    // Result publication should not fail if one notification email fails.
                }
            }

            return notifiedCount;
        }

        private string GetFrontendBaseUrl()
        {
            var configured = _configuration["Frontend:BaseUrl"];
            return string.IsNullOrWhiteSpace(configured)
                ? "http://localhost:4200"
                : configured.TrimEnd('/');
        }

        private static string GetDisplayName(User user)
        {
            var display = string.Join(
                " ",
                new[] { user.FirstName, user.MiddleName, user.LastName }
                    .Where(x => !string.IsNullOrWhiteSpace(x)));

            return string.IsNullOrWhiteSpace(display) ? user.Username : display;
        }

        private bool TryGetCurrentUserId(out int userId)
        {
            userId = 0;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("sub")?.Value;

            return int.TryParse(userIdClaim, out userId);
        }

        private bool IsCurrentUserAdmin()
        {
            if (User.IsInRole("Admin"))
                return true;

            return User.Claims.Any(c =>
                (c.Type == ClaimTypes.Role ||
                 c.Type == "role" ||
                 c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role") &&
                string.Equals(c.Value, "Admin", StringComparison.OrdinalIgnoreCase));
        }
    }
}
