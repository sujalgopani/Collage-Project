using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ExamNest.Data;
using ExamNest.Models;
using ExamNest.Models.DTOs.Exam;
using ExamNest.Models.DTOs.Student;
using ExamNest.Models.Payment;
using ExamNest.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamNest.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Student")]
    public class StudentController : ControllerBase
    {
        private static readonly HashSet<string> SevereViolationEvents = new(StringComparer.OrdinalIgnoreCase)
        {
            "fullscreen_exit",
            "devtools_opened",
            "multiple_face_detected"
        };

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
                return NotFound(new { message = "Course not found" });

            return Ok(data);
        }

        [HttpGet("my-exams")]
        public async Task<IActionResult> GetMyExams()
        {
            if (!TryGetStudentId(out var studentId))
                return Unauthorized("Invalid student token.");

            var subscribedCourseIds = _context.Subscriptions
                .Where(s => s.StudentId == studentId && s.Status == "Active")
                .Select(s => s.CourseId);

            var exams = await _context.Exams
                .Where(e => subscribedCourseIds.Contains(e.CourseId))
                .Include(e => e.Course)
                .Include(e => e.Questions)
                .OrderByDescending(e => e.StartAt)
                .Select(e => new
                {
                    e.ExamId,
                    e.CourseId,
                    CourseTitle = e.Course != null ? e.Course.Title : string.Empty,
                    e.Title,
                    e.Description,
                    e.StartAt,
                    e.EndAt,
                    e.DurationMinutes,
                    e.RandomQuestionCount,
                    examthumbail = e.Course!.ThumbailUrl,
                    TotalQuestions = e.Questions != null ? e.Questions.Count : 0,
                    AttemptStatus = _context.ExamAttempts
                    .Where(a => a.StudentId == studentId && a.ExamId == e.ExamId).Select(a => a.Status)
                    .FirstOrDefault()
                })
                .ToListAsync();

			return Ok(exams);
        }

        [HttpPost("exam/{examId}/start")]
        public async Task<IActionResult> StartExamAttempt(int examId)
        {
            if (!TryGetStudentId(out var studentId))
                return Unauthorized("Invalid student token.");

            var exam = await _context.Exams
                .Include(e => e.Course)
                .Include(e => e.Questions)
                .FirstOrDefaultAsync(e => e.ExamId == examId);

            if (exam == null)
                return NotFound("Exam not found.");

            var hasSubscription = await _context.Subscriptions.AnyAsync(s =>
                s.StudentId == studentId &&
                s.CourseId == exam.CourseId &&
                s.Status == "Active");

            if (!hasSubscription)
                return Unauthorized("Only subscribed students can attempt this exam.");

            var now = DateTime.UtcNow;
            if (now < exam.StartAt.ToUniversalTime())
                return BadRequest("Exam is not started yet.");

            if (now > exam.EndAt.ToUniversalTime())
                return BadRequest("Exam has ended.");

            var existingSubmitted = await _context.ExamAttempts
                .Where(a => a.ExamId == examId && a.StudentId == studentId && a.SubmittedAt != null)
                .AnyAsync();

            if (existingSubmitted)
                return Conflict("You have already submitted this exam.");

            var existingInProgress = await _context.ExamAttempts
                .Where(a => a.ExamId == examId && a.StudentId == studentId && a.SubmittedAt == null)
                .OrderByDescending(a => a.StartedAt)
                .FirstOrDefaultAsync();

            if (existingInProgress != null)
            {
                if (now <= existingInProgress.ExpiresAt)
                {
                    var existingPayload = BuildExamPayload(exam, existingInProgress);
                    return Ok(existingPayload);
                }

                existingInProgress.Status = "Expired";
                existingInProgress.SubmittedAt = now;
                existingInProgress.IsFlagged = true;
                existingInProgress.ViolationCount += 1;
                await _context.SaveChangesAsync();
            }

            var questions = exam.Questions?.ToList() ?? new List<ExamQuestion>();
            if (questions.Count == 0)
                return BadRequest("Exam has no questions.");

            var countPerStudent = exam.RandomQuestionCount > 0
                ? Math.Min(exam.RandomQuestionCount, questions.Count)
                : questions.Count;

            var questionIds = await GenerateQuestionOrderAsync(exam.ExamId, questions, countPerStudent);
            var questionOrderCsv = string.Join(",", questionIds);
            var selectedQuestionIdSet = questionIds.ToHashSet();

            var optionOrderMap = new Dictionary<int, List<string>>();
            foreach (var questionId in questionIds)
                optionOrderMap[questionId] = Shuffle(new List<string> { "A", "B", "C", "D" });

            var maxByDuration = now.AddMinutes(exam.DurationMinutes);
            var maxByExamWindow = exam.EndAt.ToUniversalTime();
            var expiresAt = maxByDuration <= maxByExamWindow ? maxByDuration : maxByExamWindow;

            var attempt = new ExamAttempt
            {
                ExamId = exam.ExamId,
                StudentId = studentId,
                StartedAt = now,
                ExpiresAt = expiresAt,
                Status = "InProgress",
                ClientSignature = BuildClientSignature(),
                QuestionOrderCsv = questionOrderCsv,
                OptionOrderJson = JsonSerializer.Serialize(optionOrderMap),
                MaxScore = questions.Where(q => selectedQuestionIdSet.Contains(q.ExamQuestionId)).Sum(q => q.Marks)
            };

            _context.ExamAttempts.Add(attempt);
            await _context.SaveChangesAsync();

            return Ok(BuildExamPayload(exam, attempt));
        }

        [HttpPost("exam/{examId}/submit")]
        public async Task<IActionResult> SubmitExam(int examId, [FromBody] SubmitExamRequestDto request)
        {
            if (!TryGetStudentId(out var studentId))
                return Unauthorized("Invalid student token.");

            if (request.ExamAttemptId <= 0)
                return BadRequest("ExamAttemptId is required.");

            var attempt = await _context.ExamAttempts
                .Include(a => a.Exam)
                .FirstOrDefaultAsync(a => a.ExamAttemptId == request.ExamAttemptId);

            if (attempt == null || attempt.ExamId != examId || attempt.StudentId != studentId)
                return NotFound("Exam attempt not found.");

            if (attempt.SubmittedAt != null)
                return Conflict("This attempt is already submitted.");

            if (!string.Equals(attempt.ClientSignature, BuildClientSignature(), StringComparison.Ordinal))
                return Unauthorized("Session mismatch detected. Re-login and try again.");

            var exam = await _context.Exams
                .Include(e => e.Questions)
                .FirstOrDefaultAsync(e => e.ExamId == examId);

            if (exam == null)
                return NotFound("Exam not found.");

            var now = DateTime.UtcNow;
            var isLateSubmit = now > attempt.ExpiresAt;
            if (isLateSubmit)
            {
                attempt.IsFlagged = true;
                attempt.ViolationCount += 1;
            }

            var examQuestions = (exam.Questions ?? new List<ExamQuestion>())
                .ToDictionary(q => q.ExamQuestionId, q => q);
            var selectedQuestionIds = ParseQuestionOrder(attempt.QuestionOrderCsv).ToHashSet();

            var normalizedAnswers = request.Answers
                .Where(a => examQuestions.ContainsKey(a.ExamQuestionId))
                .Where(a => selectedQuestionIds.Contains(a.ExamQuestionId))
                .Where(a => IsAllowedOption(a.SelectedOption))
                .GroupBy(a => a.ExamQuestionId)
                .ToDictionary(g => g.Key, g => g.Last().SelectedOption.Trim().ToUpperInvariant());

            var existingAnswers = _context.ExamAttemptAnswers.Where(x => x.ExamAttemptId == attempt.ExamAttemptId);
            _context.ExamAttemptAnswers.RemoveRange(existingAnswers);

            var answerEntities = new List<ExamAttemptAnswer>();
            var totalScore = 0;
            var maxScore = examQuestions.Values
                .Where(q => selectedQuestionIds.Contains(q.ExamQuestionId))
                .Sum(q => q.Marks);

            foreach (var question in examQuestions.Values.Where(q => selectedQuestionIds.Contains(q.ExamQuestionId)))
            {
                if (!normalizedAnswers.TryGetValue(question.ExamQuestionId, out var selectedOption))
                    continue;

                var isCorrect = string.Equals(
                    selectedOption,
                    question.CorrectOption.Trim().ToUpperInvariant(),
                    StringComparison.Ordinal);

                var marksAwarded = isCorrect ? question.Marks : 0;
                totalScore += marksAwarded;

                answerEntities.Add(new ExamAttemptAnswer
                {
                    ExamAttemptId = attempt.ExamAttemptId,
                    ExamQuestionId = question.ExamQuestionId,
                    SelectedOption = selectedOption,
                    IsCorrect = isCorrect,
                    MarksAwarded = marksAwarded,
                    SubmittedAt = now
                });
            }

            _context.ExamAttemptAnswers.AddRange(answerEntities);

            attempt.SubmittedAt = now;
            attempt.Status = isLateSubmit ? "SubmittedLate" : "Submitted";
            attempt.TotalScore = totalScore;
            attempt.MaxScore = maxScore;
            attempt.IsFlagged = attempt.IsFlagged || attempt.ViolationCount >= 3;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Exam submitted successfully.",
                attemptId = attempt.ExamAttemptId,
                attempt.Status,
                attempt.IsFlagged,
                attempt.ViolationCount,
                score = attempt.TotalScore,
                maxScore = attempt.MaxScore,
                submittedAt = attempt.SubmittedAt
            });
        }

        [HttpPost("exam/{examId}/report-violation")]
        public async Task<IActionResult> ReportViolation(int examId, [FromBody] ExamViolationRequestDto request)
        {
            if (!TryGetStudentId(out var studentId))
                return Unauthorized("Invalid student token.");

            if (request.ExamAttemptId <= 0 || string.IsNullOrWhiteSpace(request.EventType))
                return BadRequest("ExamAttemptId and EventType are required.");

            var attempt = await _context.ExamAttempts
                .FirstOrDefaultAsync(a => a.ExamAttemptId == request.ExamAttemptId
                    && a.ExamId == examId
                    && a.StudentId == studentId);

            if (attempt == null)
                return NotFound("Exam attempt not found.");

            if (attempt.SubmittedAt != null)
                return Conflict("Attempt is already closed.");

            if (!string.Equals(attempt.ClientSignature, BuildClientSignature(), StringComparison.Ordinal))
                return Unauthorized("Session mismatch detected.");

            var eventType = request.EventType.Trim();
            _context.ExamViolationEvents.Add(new ExamViolationEvent
            {
                ExamAttemptId = attempt.ExamAttemptId,
                EventType = eventType,
                Details = string.IsNullOrWhiteSpace(request.Details) ? null : request.Details.Trim(),
                CreatedAt = DateTime.UtcNow
            });

            attempt.ViolationCount += 1;
            if (attempt.ViolationCount >= 3 || SevereViolationEvents.Contains(eventType))
            {
                attempt.IsFlagged = true;
            }

            if (attempt.ViolationCount >= 5)
            {
                attempt.Status = "AutoTerminated";
                attempt.SubmittedAt = DateTime.UtcNow;
                attempt.IsFlagged = true;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Violation recorded.",
                attemptId = attempt.ExamAttemptId,
                attempt.Status,
                attempt.ViolationCount,
                attempt.IsFlagged
            });
        }

        [HttpGet("exam/{examId}/result/{attemptId}")]
        public async Task<IActionResult> GetExamResult(int examId, int attemptId)
        {
            if (!TryGetStudentId(out var studentId))
                return Unauthorized("Invalid student token.");

            var attempt = await _context.ExamAttempts
                .Include(a => a.Exam)
                .ThenInclude(a=>a!.Course)
                .Include(a=>a.Exam)
                .ThenInclude(a=>a!.Teacher)
                .FirstOrDefaultAsync(a => a.ExamAttemptId == attemptId
                    && a.ExamId == examId
                    && a.StudentId == studentId);
			var username = User.FindFirstValue(ClaimTypes.Name);

			if (attempt == null)
                return NotFound("Result not found.");

            if (attempt.SubmittedAt == null)
                return BadRequest("Attempt is still in progress.");

            return Ok(new
            {
                attempt.ExamAttemptId,
                attempt.ExamId,
                examTitle = attempt.Exam != null ? attempt.Exam.Title : string.Empty,
                courcename = attempt.Exam?.Course?.Title ?? "",
                teachername = attempt.Exam?.Teacher?.FirstName + attempt.Exam?.Teacher?.LastName,
				username,
				attempt.Status,
                attempt.TotalScore,
                attempt.MaxScore,
                attempt.ViolationCount,
                attempt.IsFlagged,
                attempt.StartedAt,
                attempt.SubmittedAt
			});
        }

		[HttpGet("my-attempted-exams")]
		public async Task<IActionResult> GetAttemptedExam()
		{
			if (!TryGetStudentId(out var studentId))
				return Unauthorized("Invalid student token.");

			var exams = await _context.ExamAttempts
				.Where(a => a.StudentId == studentId)
				.Include(a => a.Exam)
				.ThenInclude(e => e!.Course)
				.Include(a => a.Exam)
				.ThenInclude(e => e!.Questions)
				.OrderByDescending(a => a.StartedAt)
				.Select(a => new
				{
					a.ExamAttemptId,
					a.ExamId,
					a.Status,
					a.StartedAt,
					a.SubmittedAt,
                    a.IsFlagged,

					CourseId = a.Exam!.CourseId,
					CourseTitle = a.Exam.Course != null ? a.Exam.Course.Title : "",
					Title = a.Exam.Title,
					Description = a.Exam.Description,
					StartAt = a.Exam.StartAt,
					EndAt = a.Exam.EndAt,
					DurationMinutes = a.Exam.DurationMinutes,
					RandomQuestionCount = a.Exam.RandomQuestionCount,
					examthumbail = a.Exam.Course!.ThumbailUrl,
					TotalQuestions = a.Exam.Questions != null ? a.Exam.Questions.Count : 0
				})
				.ToListAsync();

			return Ok(exams);
		}



		//=================================================================================================

		private object BuildExamPayload(Exam exam, ExamAttempt attempt)
        {
            var orderIds = ParseQuestionOrder(attempt.QuestionOrderCsv);
            var optionOrderMap = ParseOptionOrderMap(attempt.OptionOrderJson);
            var questionMap = (exam.Questions ?? new List<ExamQuestion>())
                .ToDictionary(q => q.ExamQuestionId, q => q);

            var questions = new List<object>();
            for (var i = 0; i < orderIds.Count; i++)
            {
                if (!questionMap.TryGetValue(orderIds[i], out var q))
                    continue;

                var optionIds = optionOrderMap.TryGetValue(q.ExamQuestionId, out var stored)
                    ? stored
                    : new List<string> { "A", "B", "C", "D" };

                var options = optionIds.Select(id => new
                {
                    optionId = id,
                    text = GetOptionText(q, id)
                });

                questions.Add(new
                {
                    questionNo = i + 1,
                    q.ExamQuestionId,
                    question = q.QuestionText,
                    q.Marks,
                    options
                });
            }

            var remainingSeconds = (int)Math.Max(0, (attempt.ExpiresAt - DateTime.UtcNow).TotalSeconds);
            return new
            {
                attemptId = attempt.ExamAttemptId,
                exam.ExamId,
                exam.CourseId,
                exam.Title,
                exam.Description,
                exam.StartAt,
                exam.EndAt,
                exam.DurationMinutes,
                exam.RandomQuestionCount,
                attempt.StartedAt,
                attempt.ExpiresAt,
                remainingSeconds,
                questions
            };
        }

        private async Task<List<int>> GenerateQuestionOrderAsync(int examId, List<ExamQuestion> allQuestions, int count)
        {
            var allIds = allQuestions.Select(q => q.ExamQuestionId).ToList();
            if (count >= allIds.Count)
                return Shuffle(allIds);

            var existingOrders = await _context.ExamAttempts
                .Where(a => a.ExamId == examId && !string.IsNullOrWhiteSpace(a.QuestionOrderCsv))
                .Select(a => a.QuestionOrderCsv)
                .ToListAsync();

            var usedOrders = new HashSet<string>(existingOrders, StringComparer.Ordinal);
            for (var attempt = 0; attempt < 12; attempt++)
            {
                var candidate = Shuffle(allIds).Take(count).ToList();
                var candidateCsv = string.Join(",", candidate);
                if (!usedOrders.Contains(candidateCsv))
                    return candidate;
            }

            return Shuffle(allIds).Take(count).ToList();
        }

        private static string GetOptionText(ExamQuestion q, string optionId)
        {
            return optionId.ToUpperInvariant() switch
            {
                "A" => q.OptionA,
                "B" => q.OptionB,
                "C" => q.OptionC,
                "D" => q.OptionD,
                _ => string.Empty
            };
        }

        private static bool IsAllowedOption(string? option)
        {
            if (string.IsNullOrWhiteSpace(option))
                return false;

            var normalized = option.Trim().ToUpperInvariant();
            return normalized is "A" or "B" or "C" or "D";
        }

        private Dictionary<int, List<string>> ParseOptionOrderMap(string optionOrderJson)
        {
            if (string.IsNullOrWhiteSpace(optionOrderJson))
                return new Dictionary<int, List<string>>();

            try
            {
                var parsed = JsonSerializer.Deserialize<Dictionary<int, List<string>>>(optionOrderJson);
                return parsed ?? new Dictionary<int, List<string>>();
            }
            catch
            {
                return new Dictionary<int, List<string>>();
            }
        }

        private static List<int> ParseQuestionOrder(string csv)
        {
            if (string.IsNullOrWhiteSpace(csv))
                return new List<int>();

            return csv.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s, out var x) ? x : 0)
                .Where(x => x > 0)
                .ToList();
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

        private static List<T> Shuffle<T>(IList<T> source)
        {
            var result = source.ToList();
            for (var i = result.Count - 1; i > 0; i--)
            {
                var j = RandomNumberGenerator.GetInt32(i + 1);
                (result[i], result[j]) = (result[j], result[i]);
            }

            return result;
        }

        private string BuildClientSignature()
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown-ip";
            var ua = Request.Headers.UserAgent.ToString();
            var raw = $"{ip}|{ua}";
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
            return Convert.ToHexString(bytes);
        }

        private bool TryGetStudentId(out int studentId)
        {
            studentId = 0;
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(id, out studentId);
        }
    }
}
