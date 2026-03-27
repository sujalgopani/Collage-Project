using ExamNest.Data;
using ExamNest.Models;
using ExamNest.Models.DTOs.Exam;
using ExamNest.Models.DTOs.Student;
using ExamNest.Models.DTOs.suggestion;
using ExamNest.Models.Payment;
using ExamNest.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ExamNest.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Teacher,Admin,Student")]
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
        private readonly IEmailSender _emailSender;
        private readonly IConfiguration _configuration;
        private readonly AppActivityEmailService _activityEmailService;

        public StudentController(
            AppDbContext context,
            Student studentservice,
            IEmailSender emailSender,
            IConfiguration configuration,
            AppActivityEmailService activityEmailService)
        {
            _context = context;
            _studentservice = studentservice;
            _emailSender = emailSender;
            _configuration = configuration;
            _activityEmailService = activityEmailService;
        }

        [HttpGet("published-courses")]
        public async Task<IActionResult> GetPublishedCourses([FromQuery] string? search = null)
        {
            var query = _context.Courses
                .AsNoTracking()
                .Where(c => c.IsPublished);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                var likeTerm = $"%{term}%";
                var isNumeric = int.TryParse(term, out var courseId);

                query = query.Where(c =>
                    (isNumeric && c.CourseId == courseId) ||
                    EF.Functions.Like(c.Title, likeTerm) ||
                    EF.Functions.Like(c.Description, likeTerm));
            }

            var courses = await query
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
        public async Task<IActionResult> GetCourses([FromQuery] string? search = null)
        {
            if (!TryGetStudentId(out var studentId))
                return Unauthorized("Invalid student token.");

            var courses = await _studentservice.GetCourses(studentId, search);
            return Ok(courses);
        }

        [HttpGet("course/{courseId}/videos")]
        public async Task<IActionResult> GetCourseVideos(int courseId)
        {
            var subscription = await GetActiveSubscriptionForCurrentStudent(courseId);
            if (subscription == null)
                return Unauthorized("Only subscribed students can access this course content.");

            var course = await _context.Courses
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CourseId == courseId);
            if (course == null)
                return NotFound("Course not found.");

            if (DateTime.Now < course.StartDate)
                return BadRequest("Course not started yet");

            if (DateTime.Now > course.EndDate)
                return BadRequest("Course ended");

            var videos = await _context.CourseMedias
                .AsNoTracking()
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

        [HttpGet("live-classes")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetStudentLiveClasses()
        {
            if (!TryGetStudentId(out var studentId))
                return Unauthorized("Invalid student token.");

            var subscribedCourseIds = _context.Subscriptions
                .AsNoTracking()
                .Where(s => s.StudentId == studentId && s.Status == "Active")
                .Select(s => s.CourseId);

            var historyFrom = DateTime.UtcNow.AddDays(-30);

            var data = await _context.LiveClassSchedules
                .AsNoTracking()
                .Include(x => x.Course)
                .Include(x => x.Teacher)
                .Where(x => subscribedCourseIds.Contains(x.CourseId))
                .Where(x => !x.IsCancelled)
                .Where(x => x.EndAt >= historyFrom)
                .OrderBy(x => x.StartAt)
                .Select(x => new
                {
                    x.LiveClassScheduleId,
                    x.CourseId,
                    CourseTitle = x.Course != null ? x.Course.Title : "Course",
                    x.TeacherId,
                    TeacherName = x.Teacher != null
                        ? (
                            ((x.Teacher.FirstName ?? string.Empty) + " " + (x.Teacher.LastName ?? string.Empty)).Trim() == string.Empty
                                ? x.Teacher.Username
                                : ((x.Teacher.FirstName ?? string.Empty) + " " + (x.Teacher.LastName ?? string.Empty)).Trim()
                        )
                        : "Teacher",
                    x.Title,
                    x.Agenda,
                    x.MeetingLink,
                    x.StartAt,
                    x.EndAt,
                    x.MaterialTitle,
                    x.MaterialDescription,
                    x.MaterialLink,
                    x.MaterialFilePath
                })
                .ToListAsync();

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
                .AsNoTracking()
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

            var student = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == studentId);

            if (student != null)
            {
                await NotifyStudentExamStartedAsync(student, exam, attempt);
            }

            await _activityEmailService.NotifyAdminsAsync(
                subject: $"Activity: Exam Started - {exam.Title}",
                activityTitle: "Student Started Exam",
                summary: "A student started an exam attempt.",
                detailsRows: new Dictionary<string, string>
                {
                    ["Student"] = student != null ? GetDisplayName(student) : $"Student #{studentId}",
                    ["Exam"] = exam.Title,
                    ["Course"] = exam.Course?.Title ?? "Course",
                    ["Started At"] = attempt.StartedAt.ToString("dd MMM yyyy, hh:mm tt")
                },
                actionPathOrUrl: "/admin-dashboard/exams-manage");

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

            var submittedBy = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == studentId);

            await NotifyAdminsSafeAsync(
                subject: $"Activity: Exam Submitted - {exam.Title}",
                activityTitle: "Student Submitted Exam",
                summary: "A student submitted an exam attempt.",
                detailsRows: new Dictionary<string, string>
                {
                    ["Student"] = submittedBy != null ? GetDisplayName(submittedBy) : $"Student #{studentId}",
                    ["Exam"] = exam.Title,
                    ["Exam ID"] = exam.ExamId.ToString(),
                    ["Attempt ID"] = attempt.ExamAttemptId.ToString(),
                    ["Score"] = $"{attempt.TotalScore}/{attempt.MaxScore}",
                    ["Status"] = attempt.Status
                },
                actionPathOrUrl: "/admin-dashboard/exams-manage");

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

            var isAutoTerminated = false;
            if (attempt.ViolationCount >= 5)
            {
                var exam = await _context.Exams
                    .Include(e => e.Questions)
                    .FirstOrDefaultAsync(e => e.ExamId == examId);

                if (exam != null)
                {
                    var examQuestions = (exam.Questions ?? new List<ExamQuestion>())
                        .ToDictionary(q => q.ExamQuestionId, q => q);
                    var selectedQuestionIds = ParseQuestionOrder(attempt.QuestionOrderCsv).ToHashSet();

                    var normalizedAnswers = (request.Answers ?? new List<SubmitExamAnswerDto>())
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

                    var now = DateTime.UtcNow;
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
                    attempt.TotalScore = totalScore;
                    attempt.MaxScore = maxScore;
                }

                attempt.Status = "AutoTerminated";
                attempt.SubmittedAt = DateTime.UtcNow;
                attempt.IsFlagged = true;
                isAutoTerminated = true;
            }

            await _context.SaveChangesAsync();

            var isSevereViolation = SevereViolationEvents.Contains(eventType);
            var shouldNotify = isAutoTerminated || isSevereViolation || attempt.ViolationCount >= 3;

            if (shouldNotify)
            {
                var examMeta = await _context.Exams
                    .AsNoTracking()
                    .Include(e => e.Teacher)
                    .Include(e => e.Course)
                    .FirstOrDefaultAsync(e => e.ExamId == examId);

                var student = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.UserId == studentId);

                await NotifyAdminsSafeAsync(
                    subject: $"Activity: Exam Violation - Exam #{examId}",
                    activityTitle: "Exam Violation Reported",
                    summary: "A student triggered one or more proctoring violations during an exam.",
                    detailsRows: new Dictionary<string, string>
                    {
                        ["Student"] = student != null ? GetDisplayName(student) : $"Student #{studentId}",
                        ["Exam"] = examMeta?.Title ?? $"Exam #{examId}",
                        ["Course"] = examMeta?.Course?.Title ?? "Course",
                        ["Violation Event"] = eventType,
                        ["Violation Count"] = attempt.ViolationCount.ToString(),
                        ["Attempt Status"] = attempt.Status
                    },
                    actionPathOrUrl: "/admin-dashboard/exams-manage");

                if (examMeta?.Teacher != null && !string.IsNullOrWhiteSpace(examMeta.Teacher.Email))
                {
                    var teacherBody = EmailTemplateBuilder.BuildAdminActivityEmail(
                        activityTitle: "Exam violation alert for your student",
                        summary: "A student in your exam has triggered proctoring violations. Please review the attempt.",
                        detailsRows: new Dictionary<string, string>
                        {
                            ["Exam"] = examMeta.Title,
                            ["Course"] = examMeta.Course?.Title ?? "Course",
                            ["Student"] = student != null ? GetDisplayName(student) : $"Student #{studentId}",
                            ["Violation Event"] = eventType,
                            ["Violation Count"] = attempt.ViolationCount.ToString(),
                            ["Attempt ID"] = attempt.ExamAttemptId.ToString()
                        },
                        actionUrl: $"{GetFrontendBaseUrl()}/teacher-dashboard/studentexamwise");

                    try
                    {
                        await _emailSender.SendEmailAsync(
                            examMeta.Teacher.Email,
                            $"Exam Violation Alert: {examMeta.Title}",
                            teacherBody,
                            isBodyHtml: true);
                    }
                    catch
                    {
                        // Violation handling should not fail if teacher notification email fails.
                    }
                }
            }

            return Ok(new
            {
                message = "Violation recorded.",
                attemptId = attempt.ExamAttemptId,
                attempt.Status,
                attempt.ViolationCount,
                attempt.IsFlagged,
                isAutoTerminated
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

            if (attempt.Exam == null || !attempt.Exam.IsResultPublished)
                return BadRequest("Result is not published yet.");

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
                .AsNoTracking()
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
                    IsFlagged = a.Exam != null && a.Exam.IsResultPublished,

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

        [HttpPost("suggestion/post")]
        public async Task<IActionResult> CreateSuggestion([FromBody] SuggestionCreateDto dto)
        {
            if (!TryGetStudentId(out var studentId))
                return Unauthorized("Invalid student token.");

            if (dto == null)
                return BadRequest("Invalid suggestion payload.");

            if (dto.TeacherId <= 0)
                return BadRequest("TeacherId is required.");

            var normalizedTitle = (dto.Title ?? string.Empty).Trim();
            var normalizedMessage = (dto.Message ?? string.Empty).Trim();

            if (normalizedTitle.Length is < 5 or > 100)
                return BadRequest("Title must be between 5 and 100 characters.");

            if (normalizedMessage.Length is < 10 or > 1000)
                return BadRequest("Message must be between 10 and 1000 characters.");

            var student = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == studentId);

            var teacher = await _context.Users
                .AsNoTracking()
                .Where(u => u.UserId == dto.TeacherId)
                .Join(
                    _context.Roles.AsNoTracking(),
                    u => u.RoleId,
                    r => r.RoleId,
                    (u, r) => new { User = u, r.RoleName })
                .Where(x => x.RoleName != null && x.RoleName.ToLower() == "teacher")
                .Select(x => x.User)
                .FirstOrDefaultAsync();

            if (teacher == null)
            {
                return BadRequest("Teacher not found.");
            }

            var suggestion = new Suggestion
            {
                StudentId = studentId,
                TeacherId = dto.TeacherId,
                Title = normalizedTitle,
                Message = normalizedMessage,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.Suggestions.Add(suggestion);
            await _context.SaveChangesAsync();

            await NotifyTeacherAboutSuggestionAsync(teacher, student, suggestion);

            try
            {
                await _activityEmailService.NotifyAdminsAsync(
                    subject: $"Activity: Suggestion Submitted - {suggestion.Title ?? "Feedback"}",
                    activityTitle: "New Student Suggestion",
                    summary: "A student submitted a new suggestion.",
                    detailsRows: new Dictionary<string, string>
                    {
                        ["Student"] = student != null ? GetDisplayName(student) : $"Student #{studentId}",
                        ["Teacher"] = GetDisplayName(teacher),
                        ["Suggestion"] = suggestion.Title ?? "Feedback"
                    },
                    actionPathOrUrl: "/admin-dashboard");
            }
            catch
            {
                // Keep suggestion creation successful if activity notification fails.
            }

            return Ok(suggestion);
        }

        [HttpGet("my-teachers")]
        public async Task<IActionResult> GetMyTeachers()
        {
            if (!TryGetStudentId(out var studentId))
                return Unauthorized("Invalid student token.");

            var teachers = await (
                from s in _context.Subscriptions
                join c in _context.Courses on s.CourseId equals c.CourseId
                join u in _context.Users on c.TeacherId equals u.UserId
                join r in _context.Roles on u.RoleId equals r.RoleId
                where s.StudentId == studentId
                      && s.Status != null
                      && s.Status.Trim().ToLower() == "active"
                      && r.RoleName != null
                      && r.RoleName.ToLower() == "teacher"
                select new
                {
                    u.UserId,
                    Name = (u.FirstName + " " + u.LastName).Trim()
                }
            ).Distinct().ToListAsync();

            return Ok(teachers);
        }


        // ✅ Teacher gets suggestions
       

        // ✅ Student gets own suggestions
        [HttpGet("studentgetsuggestion")]
        public async Task<IActionResult> GetStudentSuggestions()
        {
            if (!TryGetStudentId(out var studentId))
                return Unauthorized("Invalid student token.");

            var data = await _context.Suggestions
                .AsNoTracking()
                .Include(x => x.Teacher)
                .Where(x => x.StudentId == studentId)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new
                {
                    x.Id,
                    x.Title,
                    x.Message,
                    x.Status,
                    x.Reply,
                    x.CreatedAt,
                    TeacherName = x.Teacher != null
                        ? ((x.Teacher.FirstName ?? string.Empty) + " " + (x.Teacher.LastName ?? string.Empty)).Trim()
                        : string.Empty,
                    TeacherUsername = x.Teacher != null ? x.Teacher.Username : string.Empty
                })
                .ToListAsync();

            return Ok(data);
        }


        [HttpGet("student-dashboard")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetStudentDashboard()
        {
            if (!TryGetStudentId(out var studentId))
                return Unauthorized(); // ❗ better than null

            // Total Enrolled Courses
            var totalCourses = await _context.Subscriptions
                .Where(s => s.StudentId == studentId)
                .CountAsync();

            // Active Courses
            var activeCourses = await _context.Subscriptions
                .Where(s => s.StudentId == studentId && s.Status == "Active")
                .CountAsync();

            // Total Exams Attempted
            var totalExams = await _context.ExamAttempts
                .Where(e => e.StudentId == studentId && e.SubmittedAt != null)
                .CountAsync();

            // ✅ FIXED Average Score
            var scores = await _context.ExamAttempts
                .Where(e => e.StudentId == studentId
                    && e.MaxScore > 0)
                .Select(e => new
                {
                    e.TotalScore,
                    e.MaxScore
                })
                .ToListAsync();   // 👉 move to memory

            double avgScore = 0;

            if (scores.Count > 0)
            {
                avgScore = scores
                    .Select(e => (double)e.TotalScore / e.MaxScore * 100)
                    .Average();
            }

            return Ok(new
            {
                totalCourses,
                activeCourses,
                totalExams,
                avgScore = Math.Round(avgScore, 2)
            });
        }

        [HttpGet("student-courses")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetStudentCourses()
        {
            if (!TryGetStudentId(out var studentId))
                return null!;

            var data = await _context.Subscriptions
                .Where(s => s.StudentId == studentId)
                .Select(s => new
                {
                    title = s.Course!.Title,
                    teacherName = s.Course.Teacher!.Username,

                    // Dummy progress (you can improve later)
                    progress = 50
                })
                .ToListAsync();

            return Ok(data);
        }


        [HttpGet("student-exams")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetStudentExams()
        {
            if (!TryGetStudentId(out var studentId))
                return null!;

            var data = await _context.ExamAttempts
                .Where(e => e.StudentId == studentId && e.SubmittedAt != null)
                .OrderByDescending(e => e.SubmittedAt)
                .Take(5)
                .Select(e => new
                {
                    title = e.Exam!.Title,
                    course = e.Exam.Course!.Title,

                    score = e.MaxScore > 0
                        ? (int)((double)e.TotalScore / e.MaxScore * 100)
                        : 0
                })
                .ToListAsync();

            return Ok(data);
        }



		[HttpGet("student/last-7-days-scores")]
		[Authorize(Roles = "Student")]
		public async Task<IActionResult> GetStudentLast7DaysScores()
		{
			// 🔐 Get Student Id from JWT
			var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
							  ?? User.FindFirst("sub")?.Value;

			if (!int.TryParse(userIdClaim, out int studentId))
				return Unauthorized("Invalid token");

			var today = DateTime.UtcNow.Date;
			var last7Days = today.AddDays(-6);

			var result = await _context.ExamAttempts
				.Where(a => a.StudentId == studentId)
				.Where(a => a.SubmittedAt != null)
				.Where(a => a.SubmittedAt >= last7Days && a.SubmittedAt <= today.AddDays(1))
				.Where(a => a.Status == "Submitted")
				.Select(a => new
				{
					Date = a.SubmittedAt!.Value.Date,
					Score = a.TotalScore,
					ExamTitle = a.Exam!.Title
				})
				.OrderBy(x => x.Date)
				.ToListAsync();

			return Ok(result);
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
                .AsNoTracking()
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

        private async Task NotifyTeacherAboutSuggestionAsync(User teacher, User? student, Suggestion suggestion)
        {
            if (string.IsNullOrWhiteSpace(teacher.Email))
                return;

            var teacherName = GetDisplayName(teacher);
            var studentName = student != null ? GetDisplayName(student) : "Student";
            var dashboardUrl = $"{GetFrontendBaseUrl()}/teacher-dashboard/teachersuggestion";

            var body = EmailTemplateBuilder.BuildSuggestionSubmittedEmail(
                teacherName,
                studentName,
                suggestion.Title ?? "Student suggestion",
                suggestion.Message ?? "A new suggestion was submitted.",
                dashboardUrl);

            try
            {
                await _emailSender.SendEmailAsync(
                    teacher.Email,
                    $"New Suggestion: {suggestion.Title ?? "Feedback"}",
                    body,
                    isBodyHtml: true);
            }
            catch
            {
                // Suggestion creation should remain successful even if notification fails.
            }
        }

        private async Task NotifyStudentExamStartedAsync(User student, Exam exam, ExamAttempt attempt)
        {
            if (string.IsNullOrWhiteSpace(student.Email))
                return;

            var body = EmailTemplateBuilder.BuildExamStartedEmail(
                GetDisplayName(student),
                exam.Title,
                exam.Course?.Title ?? "Course",
                attempt.StartedAt,
                attempt.ExpiresAt,
                $"{GetFrontendBaseUrl()}/student-dashboard/student-exam");

            try
            {
                await _emailSender.SendEmailAsync(
                    student.Email,
                    $"Exam Started: {exam.Title}",
                    body,
                    isBodyHtml: true);
            }
            catch
            {
                // Exam start should continue even if email delivery fails.
            }
        }

        private async Task NotifyAdminsSafeAsync(
            string subject,
            string activityTitle,
            string summary,
            IReadOnlyDictionary<string, string>? detailsRows = null,
            string? actionPathOrUrl = null)
        {
            try
            {
                await _activityEmailService.NotifyAdminsAsync(
                    subject,
                    activityTitle,
                    summary,
                    detailsRows,
                    actionPathOrUrl);
            }
            catch
            {
                // Notification failures should never block the primary API action.
            }
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

        private bool TryGetStudentId(out int studentId)
        {
            studentId = 0;
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(id, out studentId);
        }



    }
}
