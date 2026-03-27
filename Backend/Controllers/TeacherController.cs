using ExamNest.Data;
using ExamNest.Models;
using ExamNest.Models.DTOs;
using ExamNest.Models.DTOs.Exam;
using ExamNest.Models.DTOs.LiveClass;
using ExamNest.Models.DTOs.suggestion;
using ExamNest.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.FileIO;
using Org.BouncyCastle.Bcpg;
using System.Globalization;
using System.IO.Compression;
using System.Security.Claims;
using System.Xml.Linq;

namespace ExamNest.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize(Roles = "Teacher,Admin")]
    public class TeacherController : ControllerBase
    {
        private const long MaxThumbnailSizeBytes = 20L * 1024 * 1024;
        private const long MaxVideoSizeBytes = 20L * 1024 * 1024;
        private const long MaxCourseUploadRequestSizeBytes = 20L * 1024 * 1024;
        private const long MaxExamUploadSizeBytes = 20L * 1024 * 1024;
        private const long MaxMaterialUploadSizeBytes = 20L * 1024 * 1024;

        private static readonly HashSet<string> AllowedThumbnailExtensions =
            new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };

        private static readonly HashSet<string> AllowedVideoExtensions =
            new(StringComparer.OrdinalIgnoreCase) { ".mp4", ".mov", ".avi", ".mkv", ".webm" };

        private static readonly HashSet<string> AllowedThumbnailContentTypes =
            new(StringComparer.OrdinalIgnoreCase) { "image/jpeg", "image/png", "image/webp" };

        private static readonly HashSet<string> AllowedVideoContentTypes =
            new(StringComparer.OrdinalIgnoreCase) { "video/mp4", "video/quicktime", "video/x-msvideo", "video/x-matroska", "video/webm" };

        private static readonly HashSet<string> AllowedMaterialExtensions =
            new(StringComparer.OrdinalIgnoreCase) { ".pdf", ".doc", ".docx" };

        private static readonly HashSet<string> AllowedMaterialContentTypes =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "application/pdf",
                "application/msword",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
            };

        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IEmailSender _emailSender;
        private readonly IConfiguration _configuration;
        private readonly AppActivityEmailService _activityEmailService;

        public TeacherController(
            AppDbContext context,
            IWebHostEnvironment env,
            IEmailSender emailSender,
            IConfiguration configuration,
            AppActivityEmailService activityEmailService)
        {
            _context = context;
            _env = env;
            _emailSender = emailSender;
            _configuration = configuration;
            _activityEmailService = activityEmailService;
        }

        [HttpPost("create")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(MaxCourseUploadRequestSizeBytes)]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> CreateCourse([FromForm] CourseCreateDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.Description))
                return BadRequest("Title and description are required.");

            if (dto.StartDate >= dto.EndDate)
                return BadRequest("Start date must be before end date.");

            if (dto.Fees < 0)
                return BadRequest("Fees cannot be negative.");

            var teacherIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(teacherIdClaim, out var userId))
                return Unauthorized("Invalid teacher token.");
            var rootPath = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var thumbnailFolder = Path.Combine(rootPath, "CourseThumbnail");
            var videoFolder = Path.Combine(rootPath, "CourseVideos");

            var savedFiles = new List<string>();

            string thumbnailUrl = "";

            if (dto.ThumbailUrl != null)
            {
                var thumbnailExtension = Path.GetExtension(dto.ThumbailUrl.FileName);
                if (!AllowedThumbnailExtensions.Contains(thumbnailExtension))
                    return BadRequest("Thumbnail must be jpg, jpeg, png, or webp.");

                if (!string.IsNullOrWhiteSpace(dto.ThumbailUrl.ContentType) &&
                    !AllowedThumbnailContentTypes.Contains(dto.ThumbailUrl.ContentType))
                {
                    return BadRequest("Thumbnail content type is invalid.");
                }

                if (dto.ThumbailUrl.Length <= 0 || dto.ThumbailUrl.Length > MaxThumbnailSizeBytes)
                    return BadRequest("Thumbnail size must be greater than 0 and at most 20 MB.");

                if (!Directory.Exists(thumbnailFolder))
                    Directory.CreateDirectory(thumbnailFolder);

                var fileName = Guid.NewGuid() + thumbnailExtension;
                var filePath = Path.Combine(thumbnailFolder, fileName);

                await using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await dto.ThumbailUrl.CopyToAsync(stream);
                }

                savedFiles.Add(filePath);

                thumbnailUrl += "/CourseThumbnail/" + fileName;
            }

            if (dto.Files?.Count > 0)
            {
                if (!Directory.Exists(videoFolder))
                    Directory.CreateDirectory(videoFolder);

                foreach (var file in dto.Files)
                {
                    if (file == null || file.Length <= 0)
                        return BadRequest("Video files cannot be empty.");

                    if (file.Length > MaxVideoSizeBytes)
                        return BadRequest($"Video '{file.FileName}' exceeds the 20 MB size limit.");

                    var extension = Path.GetExtension(file.FileName);
                    if (!AllowedVideoExtensions.Contains(extension))
                        return BadRequest($"Video '{file.FileName}' has an unsupported format.");

                    if (!string.IsNullOrWhiteSpace(file.ContentType) &&
                        !AllowedVideoContentTypes.Contains(file.ContentType))
                    {
                        return BadRequest($"Video '{file.FileName}' has an invalid content type.");
                    }
                }
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

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Courses.Add(course);

                if (dto.Files?.Count > 0)
                {
                    foreach (var file in dto.Files)
                    {
                        var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                        var filePath = Path.Combine(videoFolder, fileName);

                        await using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream, HttpContext.RequestAborted);
                        }

                        savedFiles.Add(filePath);
                        _context.CourseMedias.Add(new CourseMedia
                        {
                            Course = course,
                            FilePath = "/CourseVideos/" + fileName
                        });
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                foreach (var filePath in savedFiles.Where(System.IO.File.Exists))
                {
                    System.IO.File.Delete(filePath);
                }

                throw;
            }

            var teacher = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == userId);

            var teacherName = teacher != null ? GetDisplayName(teacher) : $"Teacher #{userId}";

            await _activityEmailService.NotifyAdminsAsync(
                subject: $"Activity: Course Created - {course.Title}",
                activityTitle: "New Course Created",
                summary: $"{teacherName} created a new course that may require review and approval.",
                detailsRows: new Dictionary<string, string>
                {
                    ["Teacher"] = teacherName,
                    ["Course"] = course.Title,
                    ["Fees"] = $"INR {Convert.ToDecimal(course.Fees):0.00}",
                    ["Start Date"] = course.StartDate.ToString("dd MMM yyyy"),
                    ["End Date"] = course.EndDate.ToString("dd MMM yyyy"),
                    ["Initial Media Count"] = (dto.Files?.Count ?? 0).ToString()
                },
                actionPathOrUrl: "/admin-dashboard/course-manage");

            return Ok(new { message = "Course Created Successfully" });
        }

        [HttpGet("mycourses")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> GetMyCourses()
        {
            var teacherIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(teacherIdClaim, out var teacherId))
                return Unauthorized("Invalid teacher token.");

            var courses = await _context.Courses
                .AsNoTracking()
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

        [HttpPost("upload-exam-excel")]
        [Authorize(Roles = "Teacher")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(MaxExamUploadSizeBytes)]
        public async Task<IActionResult> UploadExamFromExcel([FromForm] ExamUploadFromExcelDto dto)
        {
            if (dto.ExcelFile == null || dto.ExcelFile.Length == 0)
                return BadRequest("Excel file is required.");

            if (dto.ExcelFile.Length > MaxExamUploadSizeBytes)
                return BadRequest("Excel file must be at most 20 MB.");

            if (dto.StartAt >= dto.EndAt)
                return BadRequest("Exam start time must be before end time.");

            if (dto.DurationMinutes <= 0)
                return BadRequest("DurationMinutes must be greater than 0.");

            var teacherIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(teacherIdClaim, out var teacherId))
                return Unauthorized("Invalid teacher token.");

            var ownedCourse = await _context.Courses
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CourseId == dto.CourseId && c.TeacherId == teacherId);
            if (ownedCourse == null)
                return NotFound("Course not found for this teacher.");

            List<string[]> rows;
            try
            {
                rows = await ReadRowsAsync(dto.ExcelFile);
            }
            catch (Exception ex)
            {
                return BadRequest($"Unable to read file: {ex.Message}");
            }

            if (rows.Count <= 1)
                return BadRequest("Excel must contain a header row and at least one question row.");

            var questions = new List<ExamQuestion>();
            for (var i = 1; i < rows.Count; i++)
            {
                var rowNo = i + 1;
                var row = rows[i];

                var questionText = GetCell(row, 1);
                if (string.IsNullOrWhiteSpace(questionText))
                    continue;

                var optionA = GetCell(row, 2);
                var optionB = GetCell(row, 3);
                var optionC = GetCell(row, 4);
                var optionD = GetCell(row, 5);
                var correctInput = GetCell(row, 6).ToUpperInvariant();
                var marksInput = GetCell(row, 7);

                if (string.IsNullOrWhiteSpace(optionA) ||
                    string.IsNullOrWhiteSpace(optionB) ||
                    string.IsNullOrWhiteSpace(optionC) ||
                    string.IsNullOrWhiteSpace(optionD))
                {
                    return BadRequest($"Row {rowNo}: all 4 options are required.");
                }

                var correctOption = NormalizeCorrectOption(correctInput, optionA, optionB, optionC, optionD);
                if (correctOption == null)
                    return BadRequest($"Row {rowNo}: correct answer must be A/B/C/D or exact option text.");

                var marks = 1;
                if (!string.IsNullOrWhiteSpace(marksInput) &&
                    !int.TryParse(marksInput, NumberStyles.Integer, CultureInfo.InvariantCulture, out marks))
                {
                    return BadRequest($"Row {rowNo}: marks must be numeric.");
                }

                if (marks <= 0)
                    return BadRequest($"Row {rowNo}: marks must be greater than 0.");

                questions.Add(new ExamQuestion
                {
                    QuestionText = questionText,
                    OptionA = optionA,
                    OptionB = optionB,
                    OptionC = optionC,
                    OptionD = optionD,
                    CorrectOption = correctOption,
                    Marks = marks
                });
            }

            if (questions.Count == 0)
                return BadRequest("No valid questions found in file.");

            var finalQuestionCount = dto.RandomQuestionCount <= 0
                ? questions.Count
                : Math.Min(dto.RandomQuestionCount, questions.Count);

            var exam = new Exam
            {
                CourseId = dto.CourseId,
                TeacherId = teacherId,
                Title = string.IsNullOrWhiteSpace(dto.Title) ? "Course Exam" : dto.Title.Trim(),
                Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
                StartAt = dto.StartAt,
                EndAt = dto.EndAt,
                DurationMinutes = dto.DurationMinutes,
                RandomQuestionCount = finalQuestionCount,
                CreatedAt = DateTime.UtcNow
            };

            await using var tx = await _context.Database.BeginTransactionAsync();
            _context.Exams.Add(exam);
            await _context.SaveChangesAsync();

            foreach (var q in questions)
                q.ExamId = exam.ExamId;

            _context.ExamQuestions.AddRange(questions);
            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            await NotifySubscribedStudentsForNewExamAsync(ownedCourse, exam);

            await _activityEmailService.NotifyAdminsAsync(
                subject: $"Activity: Exam Created - {exam.Title}",
                activityTitle: "New Exam Created",
                summary: $"A teacher created a new exam for course \"{ownedCourse.Title}\".",
                detailsRows: new Dictionary<string, string>
                {
                    ["Teacher ID"] = teacherId.ToString(),
                    ["Course"] = ownedCourse.Title,
                    ["Exam"] = exam.Title,
                    ["Starts"] = exam.StartAt.ToString("dd MMM yyyy, hh:mm tt"),
                    ["Ends"] = exam.EndAt.ToString("dd MMM yyyy, hh:mm tt"),
                    ["Question Count"] = questions.Count.ToString()
                },
                actionPathOrUrl: "/admin-dashboard/exams-manage");

            return Ok(new
            {
                message = "Exam created successfully from Excel.",
                examId = exam.ExamId,
                totalQuestions = questions.Count,
                questionsPerStudent = finalQuestionCount
            });
        }


        private async Task NotifySubscribedStudentsForNewExamAsync(Course course, Exam exam)
        {
            var students = await _context.Subscriptions
                .Where(s => s.CourseId == course.CourseId && s.Status == "Active")
                .Select(s => s.Student)
                .Where(u => u != null && !string.IsNullOrWhiteSpace(u.Email))
                .Distinct()
                .ToListAsync();

            if (students.Count == 0)
                return;

            foreach (var student in students)
            {
                if (student == null)
                    continue;

                try
                {
                    var studentName = GetDisplayName(student);
                    var examUrl = $"{GetFrontendBaseUrl()}/student-dashboard/student-exam";

                    var body = EmailTemplateBuilder.BuildNewExamNotificationEmail(
                        studentName,
                        course.Title,
                        exam.Title,
                        exam.StartAt,
                        exam.EndAt,
                        exam.DurationMinutes,
                        examUrl);

                    await _emailSender.SendEmailAsync(
                        student.Email,
                        $"New Exam: {exam.Title}",
                        body,
                        isBodyHtml: true);
                }
                catch
                {
                    // Continue sending notifications to other students even if one email fails.
                }
            }
        }

        private async Task NotifySubscribedStudentsForNewMediaAsync(Course course, int uploadedCount, string teacherName)
        {
            var students = await _context.Subscriptions
                .Where(s => s.CourseId == course.CourseId && s.Status == "Active")
                .Select(s => s.Student)
                .Where(u => u != null && !string.IsNullOrWhiteSpace(u.Email))
                .Distinct()
                .ToListAsync();

            if (students.Count == 0)
                return;

            foreach (var student in students)
            {
                if (student == null)
                    continue;

                var body = EmailTemplateBuilder.BuildNewMediaUploadedEmail(
                    GetDisplayName(student),
                    course.Title,
                    uploadedCount,
                    teacherName,
                    $"{GetFrontendBaseUrl()}/student-dashboard/learn-courses");

                try
                {
                    await _emailSender.SendEmailAsync(
                        student.Email,
                        $"New Course Media: {course.Title}",
                        body,
                        isBodyHtml: true);
                }
                catch
                {
                    // Continue sending notifications to other students even if one email fails.
                }
            }
        }


		// DASHBOARD SIDE 

		[HttpGet("GetTotalCourses")]
		[Authorize(Roles = "Teacher")]
		public async Task<IActionResult> GetTotalCourses()
		{
			int teacherId = GetTeacherId();

			var totalcourse = await _context.Courses
				.Where(r => r.TeacherId == teacherId)
				.CountAsync();

			return Ok(new
			{
				totalCourses = totalcourse
			});
		}

		[HttpGet("GetTotalStudent")]
		[Authorize(Roles = "Teacher")]
		public async Task<IActionResult> GetTotalStudent()
		{
			int teacherId = GetTeacherId();

			var totalStudents = await _context.Subscriptions
				.Where(s => s.Course!.TeacherId == teacherId)
				.Select(s => s.StudentId)
				.Distinct()
				.CountAsync();

			return Ok(new
			{
				totalStudents = totalStudents
			});
		}



		[HttpGet("GetTotalExam")]
		[Authorize(Roles = "Teacher")]
		public async Task<IActionResult> GetTotalExam()
		{
			int teacherId = GetTeacherId();

			var totalexam = await _context.Exams
				.Where(r => r.TeacherId == teacherId)
				.CountAsync();

			return Ok(new
			{
				totalexam = totalexam
			});
		}


		[HttpGet("GetTotalEarnings")]
		[Authorize(Roles = "Teacher")]
		public async Task<IActionResult> GetTotalEarnings()
		{
			int teacherId = GetTeacherId();

			var totalEarning = await _context.Orders
				.Where(o => o.Course!.TeacherId == teacherId && o.Status == "Paid")
				.SumAsync(o => (decimal?)o.Amount) ?? 0;

			return Ok(new
			{
				totalEarning = totalEarning
			});
		}

		[HttpGet("course/{courseId}/students")]
		public async Task<IActionResult> GetStudentsByCourse(int courseId)
		{
			var students = await (from c in _context.Courses.AsNoTracking()
							join s in _context.Subscriptions.AsNoTracking()
								on c.CourseId equals s.CourseId
							join u in _context.Users.AsNoTracking()
								on s.StudentId equals u.UserId
							where s.CourseId == courseId
							orderby c.Title, u.Username
							select new
							{
								c.CourseId,
								c.Title,
								u.UserId,
								StudentName = u.Username,
								u.Email,
								u.Phone
							}).ToListAsync();

			return Ok(students);
		}

		[HttpGet("exams/{examId}/students")]
		public async Task<IActionResult> GetStudentsByExam(int examId)
		{
			var result = await _context.ExamAttempts
                .AsNoTracking()
				.Where(e => e.ExamId == examId)
				.Join(_context.Users.AsNoTracking(),
					ea => ea.StudentId,
					u => u.UserId,
					(ea, u) => new
					{
						u.UserId,
						u.FirstName,
						u.LastName,
						u.Email,
						ea.ExamAttemptId,
						ea.StartedAt,
						ea.SubmittedAt,
						ea.Status,
						ea.TotalScore,
						ea.MaxScore
					})
				.ToListAsync();

			return Ok(result);
		}


		[HttpGet("{id}")]
		[Authorize(Roles = "Teacher,Admin")]
		public async Task<IActionResult> GetCourseDetail(int id)
		{
			var course = await _context.Courses
                .AsNoTracking()
				.Where(c => c.CourseId == id)
				.Select(c => new
				{
					c.CourseId,
					c.Title,
					c.Description,
					c.StartDate,
					c.EndDate,
					c.Fees,
					c.IsPublished,
					c.ThumbailUrl,
					Teacher = new
					{
						c.Teacher!.UserId,
						c.Teacher.FirstName,
						c.Teacher.LastName
					},
					Media = c.CourseMedias!.Select(m => new
					{
						m.CourseMediaId,
						m.FileName,
						m.FilePath,
						m.FileType
					})
				})
				.FirstOrDefaultAsync();

			if (course == null)
				return NotFound("Course not found");

			return Ok(course);
		}


        [HttpDelete("delete-course/{id}")]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            // 1. Check if course exists
            var course = await _context.Courses.FindAsync(id);
            if (course == null)
            {
                return NotFound("Course not found");
            }

            // 2. Check if any orders exist for this course
            var hasOrders = await _context.Orders.AnyAsync(o => o.CourseId == id);

            if (hasOrders)
            {
                return BadRequest("Cannot delete this course because students have already purchased it.");
            }

            // 3. Safe to delete
            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();

            await NotifyAdminsSafeAsync(
                subject: $"Activity: Course Deleted by Teacher - {course.Title}",
                activityTitle: "Course Deleted by Teacher",
                summary: "A teacher deleted one of their courses.",
                detailsRows: new Dictionary<string, string>
                {
                    ["Course"] = course.Title,
                    ["Course ID"] = course.CourseId.ToString(),
                    ["Teacher ID"] = course.TeacherId.ToString()
                },
                actionPathOrUrl: "/admin-dashboard/course-manage");

            return Ok("Course deleted successfully");
        }

        [HttpPut("{id}")]
		[Authorize(Roles = "Teacher,Admin")]
		public async Task<IActionResult> UpdateCourse(int id, [FromBody] Course model)
		{
			var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

			if (!int.TryParse(userIdClaim, out int userId))
				return Unauthorized();

			var course = await _context.Courses.FindAsync(id);

			if (course == null)
				return NotFound();

			if (course.TeacherId != userId)
				return Forbid();

			// update fields
			course.Title = model.Title;
			course.Description = model.Description;
			course.StartDate = model.StartDate;
			course.EndDate = model.EndDate;
			course.Fees = model.Fees;
			course.IsPublished = model.IsPublished;

			await _context.SaveChangesAsync();

            await NotifyAdminsSafeAsync(
                subject: $"Activity: Course Updated - {course.Title}",
                activityTitle: "Course Updated by Teacher",
                summary: "A teacher updated course details.",
                detailsRows: new Dictionary<string, string>
                {
                    ["Course"] = course.Title,
                    ["Course ID"] = course.CourseId.ToString(),
                    ["Teacher ID"] = course.TeacherId.ToString()
                },
                actionPathOrUrl: "/admin-dashboard/course-manage");

			return Ok(new { message = "Course updated successfully" });
		}


		[HttpPost("media/upload/{courseId}")]
		[Authorize(Roles = "Teacher,Admin")]
        [RequestSizeLimit(MaxCourseUploadRequestSizeBytes)]
		public async Task<IActionResult> UploadMedia(int courseId, List<IFormFile> files)
		{
            if (files == null || files.Count == 0)
                return BadRequest("At least one file is required.");

			var course = await _context.Courses
                .AsNoTracking()
                .Include(c => c.Teacher)
                .FirstOrDefaultAsync(c => c.CourseId == courseId);
			if (course == null)
				return NotFound();

            var rootPath = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
			var uploadPath = Path.Combine(rootPath, "CourseVideos");

			if (!Directory.Exists(uploadPath))
				Directory.CreateDirectory(uploadPath);

			foreach (var file in files)
			{
                if (file == null || file.Length <= 0)
                    return BadRequest("Uploaded file cannot be empty.");

                if (file.Length > MaxVideoSizeBytes)
                    return BadRequest($"File '{file.FileName}' exceeds the 20 MB size limit.");

                var extension = Path.GetExtension(file.FileName);
                var isVideo = AllowedVideoExtensions.Contains(extension);
                var isImage = AllowedThumbnailExtensions.Contains(extension);

                if (!isVideo && !isImage)
                    return BadRequest($"File '{file.FileName}' has an unsupported format.");

                if (isVideo && !string.IsNullOrWhiteSpace(file.ContentType) && !AllowedVideoContentTypes.Contains(file.ContentType))
                    return BadRequest($"Video '{file.FileName}' has an invalid content type.");

                if (isImage && !string.IsNullOrWhiteSpace(file.ContentType) && !AllowedThumbnailContentTypes.Contains(file.ContentType))
                    return BadRequest($"Image '{file.FileName}' has an invalid content type.");

				var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
				var filePath = Path.Combine(uploadPath, fileName);

				await using (var stream = new FileStream(filePath, FileMode.Create))
				{
					await file.CopyToAsync(stream, HttpContext.RequestAborted);
				}

				var media = new CourseMedia
				{
					CourseId = courseId,
					FileName = file.FileName,
					FilePath = $"CourseVideos/{fileName}",
					FileType = isVideo ? "video" : "image"
				};

				_context.CourseMedias.Add(media);
			}

			await _context.SaveChangesAsync();

            var teacherName = course.Teacher != null ? GetDisplayName(course.Teacher) : "Your teacher";
            await NotifySubscribedStudentsForNewMediaAsync(course, files.Count, teacherName);

            await _activityEmailService.NotifyAdminsAsync(
                subject: $"Activity: New Media Uploaded - {course.Title}",
                activityTitle: "Course Media Uploaded",
                summary: $"New media files were uploaded to course \"{course.Title}\".",
                detailsRows: new Dictionary<string, string>
                {
                    ["Course"] = course.Title,
                    ["Teacher"] = teacherName,
                    ["Uploaded Files"] = files.Count.ToString()
                },
                actionPathOrUrl: "/admin-dashboard/course-manage");

			return Ok(new { message = "Media uploaded successfully" });
		}



		[HttpDelete("media/{mediaId}")]
		[Authorize(Roles = "Teacher,Admin")]
		public async Task<IActionResult> DeleteMedia(int mediaId)
		{
			var media = await _context.CourseMedias
                .Include(m => m.Course)
                .FirstOrDefaultAsync(m => m.CourseMediaId == mediaId);

			if (media == null)
				return NotFound();

			var filePath = Path.Combine("wwwroot", media.FilePath.TrimStart('/'));

			if (System.IO.File.Exists(filePath))
				System.IO.File.Delete(filePath);

			_context.CourseMedias.Remove(media);
			await _context.SaveChangesAsync();

            await NotifyAdminsSafeAsync(
                subject: $"Activity: Course Media Deleted - {media.Course?.Title ?? $"Course #{media.CourseId}"}",
                activityTitle: "Course Media Deleted",
                summary: "A teacher deleted media from a course.",
                detailsRows: new Dictionary<string, string>
                {
                    ["Course"] = media.Course?.Title ?? $"Course #{media.CourseId}",
                    ["Course ID"] = media.CourseId.ToString(),
                    ["Media ID"] = media.CourseMediaId.ToString(),
                    ["Teacher ID"] = media.Course?.TeacherId.ToString() ?? "-"
                },
                actionPathOrUrl: "/admin-dashboard/course-manage");

			return Ok(new { message = "Media deleted successfully" });
		}


        [HttpGet("live-classes")]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> GetLiveClasses()
        {
            if (!TryGetCurrentUserId(out var currentUserId))
                return Unauthorized("Invalid user token.");

            var isAdmin = IsCurrentUserAdmin();
            var query = _context.LiveClassSchedules
                .AsNoTracking()
                .Include(x => x.Course)
                .Include(x => x.Teacher)
                .AsQueryable();

            if (!isAdmin)
            {
                query = query.Where(x => x.TeacherId == currentUserId);
            }

            var data = await query
                .OrderBy(x => x.StartAt)
                .Select(x => new
                {
                    x.LiveClassScheduleId,
                    x.CourseId,
                    CourseTitle = x.Course != null ? x.Course.Title : "Course",
                    x.TeacherId,
                    TeacherName = x.Teacher != null
                        ? (((x.Teacher.FirstName ?? string.Empty) + " " + (x.Teacher.LastName ?? string.Empty)).Trim() == string.Empty
                            ? x.Teacher.Username
                            : ((x.Teacher.FirstName ?? string.Empty) + " " + (x.Teacher.LastName ?? string.Empty)).Trim())
                        : "Teacher",
                    x.Title,
                    x.Agenda,
                    x.MeetingLink,
                    x.StartAt,
                    x.EndAt,
                    x.IsCancelled,
                    x.MaterialTitle,
                    x.MaterialDescription,
                    x.MaterialLink,
                    x.MaterialFilePath,
                    x.CreatedAt,
                    x.UpdatedAt
                })
                .ToListAsync();

            return Ok(data);
        }

        [HttpPost("live-classes/{liveClassId}/material")]
        [Authorize(Roles = "Teacher,Admin")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(MaxMaterialUploadSizeBytes)]
        public async Task<IActionResult> UploadLiveClassMaterial(int liveClassId, [FromForm] LiveClassMaterialUploadDto dto)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
                return Unauthorized("Invalid user token.");

            var isAdmin = IsCurrentUserAdmin();
            var liveClass = await _context.LiveClassSchedules
                .FirstOrDefaultAsync(x => x.LiveClassScheduleId == liveClassId);

            if (liveClass == null)
                return NotFound("Live class not found.");

            if (!isAdmin && liveClass.TeacherId != currentUserId)
                return Forbid();

            var materialTitle = string.IsNullOrWhiteSpace(dto.MaterialTitle) ? null : dto.MaterialTitle.Trim();
            var materialDescription = string.IsNullOrWhiteSpace(dto.MaterialDescription) ? null : dto.MaterialDescription.Trim();
            var materialLink = string.IsNullOrWhiteSpace(dto.MaterialLink) ? null : dto.MaterialLink.Trim();

            if (materialTitle != null && materialTitle.Length > 200)
                return BadRequest("Material title cannot exceed 200 characters.");

            if (materialDescription != null && materialDescription.Length > 1000)
                return BadRequest("Material description cannot exceed 1000 characters.");

            if (materialLink != null &&
                (!Uri.TryCreate(materialLink, UriKind.Absolute, out var parsedLink) ||
                 (parsedLink.Scheme != Uri.UriSchemeHttp && parsedLink.Scheme != Uri.UriSchemeHttps)))
            {
                return BadRequest("Material link must be a valid http/https URL.");
            }

            if (dto.MaterialFile == null)
                return BadRequest("Material file is required. Upload a PDF or DOC file.");

            if (dto.MaterialFile.Length <= 0 || dto.MaterialFile.Length > MaxMaterialUploadSizeBytes)
                return BadRequest("Material file size must be greater than 0 and at most 20 MB.");

            var extension = Path.GetExtension(dto.MaterialFile.FileName);
            if (!AllowedMaterialExtensions.Contains(extension))
                return BadRequest("Only PDF, DOC, and DOCX files are allowed.");

            if (!string.IsNullOrWhiteSpace(dto.MaterialFile.ContentType) &&
                !AllowedMaterialContentTypes.Contains(dto.MaterialFile.ContentType))
            {
                return BadRequest("Material file content type is invalid.");
            }

            var rootPath = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var uploadPath = Path.Combine(rootPath, "LiveClassMaterials");
            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            var newFileName = $"{Guid.NewGuid()}{extension}";
            var fullPath = Path.Combine(uploadPath, newFileName);

            await using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await dto.MaterialFile.CopyToAsync(stream, HttpContext.RequestAborted);
            }

            if (!string.IsNullOrWhiteSpace(liveClass.MaterialFilePath))
            {
                var existingPath = Path.Combine(
                    rootPath,
                    liveClass.MaterialFilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(existingPath))
                    System.IO.File.Delete(existingPath);
            }

            liveClass.MaterialFilePath = $"/LiveClassMaterials/{newFileName}";

            liveClass.MaterialTitle = materialTitle;
            liveClass.MaterialDescription = materialDescription;
            liveClass.MaterialLink = materialLink;
            liveClass.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await NotifyStudentsForLiveClassMaterialAsync(liveClass, materialTitle ?? liveClass.MaterialTitle ?? "New Material");

            await NotifyAdminsSafeAsync(
                subject: $"Activity: Live Class Material Updated - {liveClass.Title}",
                activityTitle: "Live Class Material Updated",
                summary: "A teacher updated live class learning material.",
                detailsRows: new Dictionary<string, string>
                {
                    ["Live Class"] = liveClass.Title,
                    ["Live Class ID"] = liveClass.LiveClassScheduleId.ToString(),
                    ["Course ID"] = liveClass.CourseId.ToString(),
                    ["Teacher ID"] = liveClass.TeacherId.ToString(),
                    ["Material Title"] = liveClass.MaterialTitle ?? "Not specified"
                },
                actionPathOrUrl: "/admin-dashboard/live-classes");

            return Ok(new
            {
                message = "Live class material updated successfully.",
                liveClass = new
                {
                    liveClass.LiveClassScheduleId,
                    liveClass.MaterialTitle,
                    liveClass.MaterialDescription,
                    liveClass.MaterialLink,
                    liveClass.MaterialFilePath,
                    liveClass.UpdatedAt
                }
            });
        }


		// dashboard
		[HttpGet("recent-exams")]
		[Authorize(Roles = "Teacher")]
		public async Task<IActionResult> GetRecentExams()
		{
			// ?? Get userId from JWT
			var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
							  ?? User.FindFirst("sub")?.Value;

			if (!int.TryParse(userIdClaim, out int teacherId))
				return Unauthorized("Invalid token");

			var exams = await _context.Exams
                .AsNoTracking()
				.Where(e => e.TeacherId == teacherId)
				.OrderByDescending(e => e.CreatedAt) // latest first
				.Take(5)
				.Select(e => new
				{
					e.ExamId,
					e.Title,
					e.StartAt,
					e.EndAt,
					e.DurationMinutes,
					e.CreatedAt
				})
				.ToListAsync();

			return Ok(exams);
		}

		[HttpGet("recent-subscribers")]
		[Authorize(Roles = "Teacher")]
		public async Task<IActionResult> GetRecentSubscribers()
		{
			// ?? Get teacherId from JWT
			var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
							  ?? User.FindFirst("sub")?.Value;

			if (!int.TryParse(userIdClaim, out int teacherId))
				return Unauthorized("Invalid token");

			var subscribers = await _context.Subscriptions
                .AsNoTracking()
				.Where(s => s.Course!.TeacherId == teacherId) // ?? only my courses
				.OrderByDescending(s => s.CreatedAt) // latest first
				.Take(5)
				.Select(s => new
				{
					StudentId = s.StudentId,
					StudentName = s.Student!.FirstName + " " + s.Student.LastName,
					Email = s.Student.Email,
					CourseId = s.CourseId,
					CourseTitle = s.Course!.Title,
					Status = s.Status,
					JoinedAt = s.CreatedAt
				})
				.ToListAsync();

			return Ok(subscribers);
		}


        [HttpGet("teachergetsuggestion")]
        public async Task<IActionResult> GetTeacherSuggestions()
        {
            if (!TryGetCurrentUserId(out var currentUserId))
                return Unauthorized("Invalid user token.");

            var isAdmin = IsCurrentUserAdmin();
            var query = _context.Suggestions
                .AsNoTracking()
                .Include(x => x.Student)
                .Include(x => x.Teacher)
                .AsQueryable();

            if (!isAdmin)
            {
                query = query.Where(x => x.TeacherId == currentUserId);
            }

            var data = await query
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new
                {
                    x.Id,
                    x.Title,
                    x.Message,
                    x.Status,
                    x.Reply,
                    x.CreatedAt,
                    StudentName = x.Student != null
                        ? (
                            ((x.Student.FirstName ?? string.Empty) + " " + (x.Student.LastName ?? string.Empty)).Trim() == string.Empty
                                ? x.Student.Username
                                : ((x.Student.FirstName ?? string.Empty) + " " + (x.Student.LastName ?? string.Empty)).Trim()
                        )
                        : "Student",
                    TeacherName = x.Teacher != null
                        ? (
                            ((x.Teacher.FirstName ?? string.Empty) + " " + (x.Teacher.LastName ?? string.Empty)).Trim() == string.Empty
                                ? x.Teacher.Username
                                : ((x.Teacher.FirstName ?? string.Empty) + " " + (x.Teacher.LastName ?? string.Empty)).Trim()
                        )
                        : "Teacher"
                })
                .ToListAsync();

            return Ok(data);
        }

        [HttpPost("reply")]
        public async Task<IActionResult> ReplySuggestion([FromBody] SuggestionReplyDto dto)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
                return Unauthorized("Invalid user token.");

            if (dto == null || dto.Id <= 0)
                return BadRequest("Invalid suggestion payload.");

            var normalizedReply = (dto.Reply ?? string.Empty).Trim();
            if (normalizedReply.Length is < 3 or > 500)
                return BadRequest("Reply must be between 3 and 500 characters.");

            var isAdmin = IsCurrentUserAdmin();
            var suggestion = await _context.Suggestions
                .Include(s => s.Student)
                .Include(s => s.Teacher)
                .FirstOrDefaultAsync(s => s.Id == dto.Id);

            if (suggestion == null)
                return NotFound("Suggestion not found.");

            if (!isAdmin && suggestion.TeacherId != currentUserId)
                return Forbid();

            suggestion.Reply = normalizedReply;
            suggestion.Status = "Resolved";

            await _context.SaveChangesAsync();

            if (suggestion.Student != null && !string.IsNullOrWhiteSpace(suggestion.Student.Email))
            {
                var studentName = GetDisplayName(suggestion.Student);
                var teacherName = suggestion.Teacher != null ? GetDisplayName(suggestion.Teacher) : "Your teacher";
                var suggestionsUrl = $"{GetFrontendBaseUrl()}/student-dashboard/mysuggestion";

                var body = EmailTemplateBuilder.BuildSuggestionReplyEmail(
                    studentName,
                    teacherName,
                    suggestion.Title ?? "Suggestion",
                    suggestion.Reply ?? string.Empty,
                    suggestionsUrl);

                try
                {
                    await _emailSender.SendEmailAsync(
                        suggestion.Student.Email,
                        $"Reply to your suggestion: {suggestion.Title ?? "Feedback"}",
                        body,
                        isBodyHtml: true);
                }
                catch
                {
                    // Reply should be saved even if email delivery fails.
                }
            }

            try
            {
                await _activityEmailService.NotifyAdminsAsync(
                    subject: $"Activity: Suggestion Resolved - {suggestion.Title ?? "Feedback"}",
                    activityTitle: "Suggestion Resolved",
                    summary: "A teacher replied to a student suggestion.",
                    detailsRows: new Dictionary<string, string>
                    {
                        ["Suggestion"] = suggestion.Title ?? "Feedback",
                        ["Teacher ID"] = suggestion.TeacherId.ToString(),
                        ["Student ID"] = suggestion.StudentId.ToString(),
                        ["Status"] = suggestion.Status ?? "Resolved"
                    },
                    actionPathOrUrl: "/admin-dashboard");
            }
            catch
            {
                // Reply should remain successful even if admin activity notification fails.
            }

            return Ok(new
            {
                message = "Reply sent successfully.",
                suggestion = new
                {
                    suggestion.Id,
                    suggestion.Status,
                    suggestion.Reply
                }
            });
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteSuggestion(int id)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
                return Unauthorized("Invalid user token.");

            var isAdmin = IsCurrentUserAdmin();
            var data = await _context.Suggestions.FirstOrDefaultAsync(s => s.Id == id);

            if (data == null)
                return NotFound("Suggestion not found");

            if (!isAdmin && data.TeacherId != currentUserId)
                return Forbid();

            _context.Suggestions.Remove(data);
            await _context.SaveChangesAsync();

            await NotifyAdminsSafeAsync(
                subject: $"Activity: Suggestion Deleted - #{id}",
                activityTitle: "Suggestion Deleted",
                summary: "A suggestion was deleted by a teacher/admin.",
                detailsRows: new Dictionary<string, string>
                {
                    ["Suggestion ID"] = id.ToString(),
                    ["Teacher ID"] = data.TeacherId.ToString(),
                    ["Student ID"] = data.StudentId.ToString()
                },
                actionPathOrUrl: "/admin-dashboard");

            return Ok(new { message ="Deleted Successfully" });
        }



        // ===============================================

        private async Task NotifyStudentsForLiveClassMaterialAsync(LiveClassSchedule liveClass, string materialTitle)
        {
            var course = await _context.Courses
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CourseId == liveClass.CourseId);

            if (course == null)
                return;

            var students = await _context.Subscriptions
                .AsNoTracking()
                .Where(s => s.CourseId == liveClass.CourseId && s.Status == "Active")
                .Select(s => s.Student)
                .Where(s => s != null && !string.IsNullOrWhiteSpace(s.Email))
                .Distinct()
                .ToListAsync();

            if (students.Count == 0)
                return;

            var url = $"{GetFrontendBaseUrl()}/student-dashboard/live-classes";
            foreach (var student in students)
            {
                if (student == null || string.IsNullOrWhiteSpace(student.Email))
                    continue;

                var body = EmailTemplateBuilder.BuildLiveClassMaterialUpdatedEmail(
                    GetDisplayName(student),
                    course.Title,
                    liveClass.Title,
                    materialTitle,
                    url);

                try
                {
                    await _emailSender.SendEmailAsync(
                        student.Email,
                        $"Live Class Material Updated: {liveClass.Title}",
                        body,
                        isBodyHtml: true);
                }
                catch
                {
                    // Continue notifying remaining students even if one email fails.
                }
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

        private bool TryGetCurrentUserId(out int userId)
        {
            userId = 0;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("sub")?.Value;

            return int.TryParse(userIdClaim, out userId);
        }


        private int GetTeacherId()
		{
			var teacherId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
							?? User.FindFirst("sub")?.Value;

			return int.Parse(teacherId!);
		}


        private static string GetCell(string[] row, int oneBasedIndex)
        {
            var idx = oneBasedIndex - 1;
            if (idx < 0 || idx >= row.Length)
                return string.Empty;

            return row[idx].Trim();
        }

        private static string? NormalizeCorrectOption(
            string correctInput,
            string optionA,
            string optionB,
            string optionC,
            string optionD)
        {
            if (correctInput is "A" or "B" or "C" or "D")
                return correctInput;

            if (string.Equals(correctInput, optionA, StringComparison.OrdinalIgnoreCase)) return "A";
            if (string.Equals(correctInput, optionB, StringComparison.OrdinalIgnoreCase)) return "B";
            if (string.Equals(correctInput, optionC, StringComparison.OrdinalIgnoreCase)) return "C";
            if (string.Equals(correctInput, optionD, StringComparison.OrdinalIgnoreCase)) return "D";

            return null;
        }

        private static async Task<List<string[]>> ReadRowsAsync(IFormFile file)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            await using var stream = file.OpenReadStream();

            return ext switch
            {
                ".csv" => ReadCsvRows(stream),
                ".xlsx" => ReadXlsxRows(stream),
                _ => throw new InvalidOperationException("Only .xlsx or .csv files are supported.")
            };
        }

        private static List<string[]> ReadCsvRows(Stream stream)
        {
            var rows = new List<string[]>();
            using var parser = new TextFieldParser(stream)
            {
                Delimiters = new[] { "," },
                HasFieldsEnclosedInQuotes = true,
                TrimWhiteSpace = true
            };

            while (!parser.EndOfData)
            {
                var fields = parser.ReadFields() ?? Array.Empty<string>();
                rows.Add(fields);
            }

            return rows;
        }

        private static List<string[]> ReadXlsxRows(Stream stream)
        {
            const string nsValue = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            XNamespace ns = nsValue;

            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
            var sheetEntry = archive.GetEntry("xl/worksheets/sheet1.xml")
                             ?? archive.Entries.FirstOrDefault(e => e.FullName.StartsWith("xl/worksheets/sheet"));
            if (sheetEntry == null)
                throw new InvalidOperationException("Worksheet not found in .xlsx file.");

            var sharedStrings = ReadSharedStrings(archive, ns);

            using var sheetStream = sheetEntry.Open();
            var sheet = XDocument.Load(sheetStream);
            var sheetData = sheet.Descendants(ns + "sheetData").FirstOrDefault();
            if (sheetData == null)
                return new List<string[]>();

            var rows = new List<string[]>();
            foreach (var row in sheetData.Elements(ns + "row"))
            {
                var cells = new Dictionary<int, string>();
                foreach (var cell in row.Elements(ns + "c"))
                {
                    var cellRef = (string?)cell.Attribute("r");
                    var col = GetColumnIndex(cellRef);
                    if (col <= 0) continue;
                    cells[col] = ExtractCellValue(cell, ns, sharedStrings);
                }

                if (cells.Count == 0)
                    continue;

                var maxCol = cells.Keys.Max();
                var rowData = new string[maxCol];
                for (var c = 1; c <= maxCol; c++)
                    rowData[c - 1] = cells.TryGetValue(c, out var v) ? v : string.Empty;

                rows.Add(rowData);
            }

            return rows;
        }

        private static List<string> ReadSharedStrings(ZipArchive archive, XNamespace ns)
        {
            var entry = archive.GetEntry("xl/sharedStrings.xml");
            if (entry == null)
                return new List<string>();

            using var stream = entry.Open();
            var doc = XDocument.Load(stream);
            return doc.Descendants(ns + "si")
                .Select(si => string.Concat(si.Descendants(ns + "t").Select(t => t.Value)))
                .ToList();
        }

        private static int GetColumnIndex(string? cellRef)
        {
            if (string.IsNullOrWhiteSpace(cellRef))
                return 0;

            var letters = new string(cellRef.TakeWhile(char.IsLetter).ToArray()).ToUpperInvariant();
            if (letters.Length == 0)
                return 0;

            var col = 0;
            foreach (var ch in letters)
                col = (col * 26) + (ch - 'A' + 1);

            return col;
        }

        private static string ExtractCellValue(XElement cell, XNamespace ns, List<string> sharedStrings)
        {
            var type = (string?)cell.Attribute("t");

            if (type == "inlineStr")
            {
                return cell.Element(ns + "is")?.Element(ns + "t")?.Value?.Trim() ?? string.Empty;
            }

            var raw = cell.Element(ns + "v")?.Value?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(raw))
                return string.Empty;

            if (type == "s" && int.TryParse(raw, out var sharedIdx))
            {
                if (sharedIdx >= 0 && sharedIdx < sharedStrings.Count)
                    return sharedStrings[sharedIdx].Trim();
            }

            return raw;
        }

    }
}
