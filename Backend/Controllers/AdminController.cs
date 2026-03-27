using System.Security.Claims;
using ExamNest.Data;
using ExamNest.Models;
using ExamNest.Models.DTOs;
using ExamNest.Models.DTOs.Exam;
using ExamNest.Models.DTOs.LiveClass;
using ExamNest.Models.DTOs.Payment;
using ExamNest.Models.DTOs.User;
using ExamNest.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using Razorpay.Api;

namespace ExamNest.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
	//[Authorize(Roles ="Admin")]
	public class AdminController : Controller
    {
        private const long MaxProfileImageSizeBytes = 20L * 1024 * 1024;
        private static readonly HashSet<string> AllowedProfileImageExtensions =
            new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };

        private static readonly HashSet<string> AllowedProfileImageContentTypes =
            new(StringComparer.OrdinalIgnoreCase) { "image/jpeg", "image/png", "image/webp" };

        private readonly AdminServices _adminServices;
        private readonly AppDbContext _context;
		private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;
        private readonly IEmailSender _emailSender;
        private readonly AppActivityEmailService _activityEmailService;

		public AdminController(
            AdminServices adminServices,
            AppDbContext context,
            IConfiguration config,
            IWebHostEnvironment env,
            IEmailSender emailSender,
            AppActivityEmailService activityEmailService)
        {
            _adminServices = adminServices;
            _context = context;
			_config = config;
            _env = env;
            _emailSender = emailSender;
            _activityEmailService = activityEmailService;
        }

        // teacher side
        [HttpGet]
		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> GetAllTeacher([FromQuery] string? search = null)
        {
            var teachers = await _adminServices.GetAllTeacher(search);
            return Ok(teachers);
        }



        [HttpGet]
		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> GetAllStudent([FromQuery] string? search = null)
        {
            var teachers = await _adminServices.GetAllStudent(search);
            return Ok(teachers);
        }

        [HttpGet("{id}")]
		[Authorize(Roles = "Admin")]

		public async Task<IActionResult> GetUserById(int id)
        {
            var teacher = await _adminServices.GetByIdAsync(id);
            if (teacher == null) return NotFound();

            return Ok(teacher);
        }

        [HttpPost("AddTeacher")]
		[Authorize(Roles = "Admin")]

		public async Task<IActionResult> CreateUser(UserCreateDTO dto)
        {
            var teacher = await _adminServices.CreateAsync(dto);

            if (!string.IsNullOrWhiteSpace(teacher.Email))
            {
                var body = EmailTemplateBuilder.BuildAdminActivityEmail(
                    activityTitle: $"Hi {GetDisplayName(teacher)}, your teacher account is ready",
                    summary: "An administrator created your teacher account on ExamNest.",
                    detailsRows: new Dictionary<string, string>
                    {
                        ["Email"] = teacher.Email,
                        ["Role"] = "Teacher",
                        ["Status"] = teacher.IsActive ? "Active" : "Inactive"
                    },
                    actionUrl: $"{GetFrontendBaseUrl()}/login");

                try
                {
                    await _emailSender.SendEmailAsync(
                        teacher.Email,
                        "Your ExamNest teacher account has been created",
                        body,
                        isBodyHtml: true);
                }
                catch
                {
                    // Keep teacher creation successful if welcome email fails.
                }
            }

            await NotifyAdminsSafeAsync(
                subject: $"Activity: Teacher Created - {GetDisplayName(teacher)}",
                activityTitle: "Teacher Account Created",
                summary: "An admin created a new teacher account.",
                detailsRows: new Dictionary<string, string>
                {
                    ["Teacher"] = GetDisplayName(teacher),
                    ["Email"] = teacher.Email,
                    ["Teacher ID"] = teacher.UserId.ToString()
                },
                actionPathOrUrl: "/admin-dashboard/teacher-manage");

            return Ok(teacher);
        }

        [HttpPut("{id}")]
		[Authorize(Roles = "Admin")]

		public async Task<IActionResult> UpdateUser(int id, UserUpdateDTO dto)
        {
            var result = await _adminServices.UpdateAsync(id, dto);
            if (!result) return NotFound();

            var teacher = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == id && u.RoleId == 2);

            if (teacher != null)
            {
                if (!string.IsNullOrWhiteSpace(teacher.Email))
                {
                    var body = EmailTemplateBuilder.BuildAdminActivityEmail(
                        activityTitle: $"Hi {GetDisplayName(teacher)}, your profile was updated",
                        summary: "An administrator updated your teacher account details.",
                        detailsRows: new Dictionary<string, string>
                        {
                            ["Teacher"] = GetDisplayName(teacher),
                            ["Email"] = teacher.Email
                        },
                        actionUrl: $"{GetFrontendBaseUrl()}/teacher-dashboard/teacherprofile");

                    try
                    {
                        await _emailSender.SendEmailAsync(
                            teacher.Email,
                            "Your teacher profile was updated",
                            body,
                            isBodyHtml: true);
                    }
                    catch
                    {
                        // Keep profile update successful if notification email fails.
                    }
                }

                await NotifyAdminsSafeAsync(
                    subject: $"Activity: Teacher Updated - {GetDisplayName(teacher)}",
                    activityTitle: "Teacher Account Updated",
                    summary: "An admin updated teacher details.",
                    detailsRows: new Dictionary<string, string>
                    {
                        ["Teacher"] = GetDisplayName(teacher),
                        ["Teacher ID"] = teacher.UserId.ToString()
                    },
                    actionPathOrUrl: "/admin-dashboard/teacher-manage");
            }

            return Ok("Teacher Updated Successfully");
        }

        [HttpPut("{id}")] // student		
		[Authorize(Roles = "Admin")]

		public async Task<IActionResult> UpdateStudent(int id, UserUpdateDTO dto)
        {
            var result = await _adminServices.UpdateStudentAsync(id, dto);
            if (!result) return NotFound();

            var student = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == id && u.RoleId == 3);

            if (student != null)
            {
                if (!string.IsNullOrWhiteSpace(student.Email))
                {
                    var body = EmailTemplateBuilder.BuildAdminActivityEmail(
                        activityTitle: $"Hi {GetDisplayName(student)}, your profile was updated",
                        summary: "An administrator updated your student account details.",
                        detailsRows: new Dictionary<string, string>
                        {
                            ["Student"] = GetDisplayName(student),
                            ["Email"] = student.Email
                        },
                        actionUrl: $"{GetFrontendBaseUrl()}/student-dashboard/studentprofile");

                    try
                    {
                        await _emailSender.SendEmailAsync(
                            student.Email,
                            "Your student profile was updated",
                            body,
                            isBodyHtml: true);
                    }
                    catch
                    {
                        // Keep profile update successful if notification email fails.
                    }
                }

                await NotifyAdminsSafeAsync(
                    subject: $"Activity: Student Updated - {GetDisplayName(student)}",
                    activityTitle: "Student Account Updated",
                    summary: "An admin updated student details.",
                    detailsRows: new Dictionary<string, string>
                    {
                        ["Student"] = GetDisplayName(student),
                        ["Student ID"] = student.UserId.ToString()
                    },
                    actionPathOrUrl: "/admin-dashboard/student-manage");
            }

			return Ok(new
			{
				success = true,
				message = "Student updated successfully"
			});
		}



		[HttpDelete("{id}")]
		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> DeleteUser(int id)
		{
			// ✅ Get teacher
			var teacher = await _context.Users
				.FirstOrDefaultAsync(u => u.UserId == id && u.RoleId == 2);

			if (teacher == null)
				return NotFound("Teacher not found.");

			// ✅ CHECK: Teacher has courses with orders
			var hasOrders = await _context.Orders
				.AnyAsync(o => o.Course!.TeacherId == id);

			if (hasOrders)
			{
				return BadRequest(new
				{
					message = "Cannot delete this teacher because students have purchased their courses."
				});
			}

			// ✅ OPTIONAL: check only courses (even without orders)
			var hasCourses = await _context.Courses
				.AnyAsync(c => c.TeacherId == id);

			if (hasCourses)
			{
				return BadRequest(new
				{
					message = "Cannot delete this teacher because they have active courses."
				});
			}

			// ✅ SAFE DELETE
			var result = await _adminServices.DeleteAsync(id);
			if (!result) return NotFound();

			// ✅ EMAIL TO TEACHER
			if (!string.IsNullOrWhiteSpace(teacher.Email))
			{
				var body = EmailTemplateBuilder.BuildAdminActivityEmail(
					activityTitle: $"Hi {GetDisplayName(teacher)}, your account was removed",
					summary: "An administrator removed your teacher account from ExamNest.",
					detailsRows: new Dictionary<string, string>
					{
						["Teacher"] = GetDisplayName(teacher),
						["Email"] = teacher.Email
					});

				try
				{
					await _emailSender.SendEmailAsync(
						teacher.Email,
						"Your teacher account was removed",
						body,
						isBodyHtml: true);
				}
				catch { }
			}

			// ✅ NOTIFY ADMINS
			await NotifyAdminsSafeAsync(
				subject: $"Activity: Teacher Deleted - #{id}",
				activityTitle: "Teacher Account Deleted",
				summary: "An admin deleted a teacher account.",
				detailsRows: new Dictionary<string, string>
				{
					["Teacher"] = GetDisplayName(teacher),
					["Teacher ID"] = id.ToString()
				},
				actionPathOrUrl: "/admin-dashboard/teacher-manage");

			return Ok(new { message = "Teacher deleted successfully." });
		}

		
        [HttpDelete("{id}")]
		[Authorize(Roles = "Admin")]

		public async Task<IActionResult> DeleteStudent(int id)
        {
            var student = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == id && u.RoleId == 3);

            var hasSubscriptions = await _context.Subscriptions
                .AsNoTracking()
                .AnyAsync(s => s.StudentId == id);

            var hasExamAttempts = await _context.ExamAttempts
                .AsNoTracking()
                .AnyAsync(a => a.StudentId == id);

            if (hasSubscriptions || hasExamAttempts)
            {
                if (hasSubscriptions && hasExamAttempts)
                    return BadRequest("Cannot delete this student because the student is subscribed to courses and has exam attempts.");

                if (hasSubscriptions)
                    return BadRequest("Cannot delete this student because the student is subscribed to course(s).");

                return BadRequest("Cannot delete this student because the student has exam attempts.");
            }

            try
            {
                var result = await _adminServices.DeleteStudentAsync(id);
                if (!result) return NotFound();

                if (student != null && !string.IsNullOrWhiteSpace(student.Email))
                {
                    var body = EmailTemplateBuilder.BuildAdminActivityEmail(
                        activityTitle: $"Hi {GetDisplayName(student)}, your account was removed",
                        summary: "An administrator removed your student account from ExamNest.",
                        detailsRows: new Dictionary<string, string>
                        {
                            ["Student"] = GetDisplayName(student),
                            ["Email"] = student.Email
                        });

                    try
                    {
                        await _emailSender.SendEmailAsync(
                            student.Email,
                            "Your student account was removed",
                            body,
                            isBodyHtml: true);
                    }
                    catch
                    {
                        // Keep delete operation successful if notification email fails.
                    }
                }

                await NotifyAdminsSafeAsync(
                    subject: $"Activity: Student Deleted - #{id}",
                    activityTitle: "Student Account Deleted",
                    summary: "An admin deleted a student account.",
                    detailsRows: new Dictionary<string, string>
                    {
                        ["Student"] = student != null ? GetDisplayName(student) : $"Student #{id}",
                        ["Student ID"] = id.ToString()
                    },
                    actionPathOrUrl: "/admin-dashboard/student-manage");

                return Ok("Deleted Successfully");
            }
            catch (DbUpdateException)
            {
                return BadRequest("Cannot delete this student because related records exist.");
            }
        }

        // course side

        [HttpGet]
		[Authorize(Roles = "Admin")]

        public async Task<IActionResult> GetAllCourses([FromQuery] string? search = null)
        {
            var query = _context.Courses
                .AsNoTracking()
                .Include(c => c.Teacher)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                var likeTerm = $"%{term}%";
                var isNumeric = int.TryParse(term, out var courseId);

                query = query.Where(c =>
                    (isNumeric && c.CourseId == courseId) ||
                    EF.Functions.Like(c.Title, likeTerm) ||
                    EF.Functions.Like(c.Description, likeTerm) ||
                    (c.Teacher != null && EF.Functions.Like(c.Teacher.Username, likeTerm)) ||
                    (c.Teacher != null && EF.Functions.Like(c.Teacher.FirstName + " " + c.Teacher.LastName, likeTerm)));
            }

			var result = await query
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new
                {
                    c.CourseId,
                    c.Title,
                    c.Description,
                    c.Fees,
                    c.StartDate,
                    c.EndDate,
                    c.IsPublished,
                    c.CreatedAt,
                    TeacherName = c.Teacher != null ? c.Teacher.Username : "Unknown",
                })
                .ToListAsync();

            return Ok(result);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GlobalSearch([FromQuery] string? q, [FromQuery] int take = 5)
        {
            var term = q?.Trim();
            if (string.IsNullOrWhiteSpace(term))
            {
                return Ok(new
                {
                    query = string.Empty,
                    courses = Array.Empty<object>(),
                    teachers = Array.Empty<object>(),
                    students = Array.Empty<object>(),
                    exams = Array.Empty<object>(),
                    totalMatches = 0
                });
            }

            take = Math.Clamp(take, 1, 20);

            var likeTerm = $"%{term}%";
            var isNumeric = int.TryParse(term, out var numericId);

            var courses = await _context.Courses
                .AsNoTracking()
                .Include(c => c.Teacher)
                .Where(c =>
                    (isNumeric && c.CourseId == numericId) ||
                    EF.Functions.Like(c.Title, likeTerm) ||
                    EF.Functions.Like(c.Description, likeTerm) ||
                    (c.Teacher != null && (
                        EF.Functions.Like(c.Teacher.Username, likeTerm) ||
                        EF.Functions.Like(c.Teacher.FirstName, likeTerm) ||
                        EF.Functions.Like(c.Teacher.LastName, likeTerm))))
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new
                {
                    c.CourseId,
                    c.Title,
                    c.IsPublished,
                    TeacherName = c.Teacher != null
                        ? $"{c.Teacher.FirstName} {c.Teacher.LastName}".Trim()
                        : "Unknown"
                })
                .Take(take)
                .ToListAsync();

            var teachers = await _context.Users
                .AsNoTracking()
                .Where(u => u.RoleId == 2)
                .Where(u =>
                    (isNumeric && u.UserId == numericId) ||
                    EF.Functions.Like(u.FirstName, likeTerm) ||
                    (u.MiddleName != null && EF.Functions.Like(u.MiddleName, likeTerm)) ||
                    EF.Functions.Like(u.LastName, likeTerm) ||
                    EF.Functions.Like(u.Email, likeTerm) ||
                    EF.Functions.Like(u.Username, likeTerm) ||
                    (u.Phone != null && EF.Functions.Like(u.Phone, likeTerm)))
                .OrderByDescending(u => u.UserId)
                .Select(u => new
                {
                    u.UserId,
                    u.RoleId,
                    FullName = $"{u.FirstName} {u.LastName}".Trim(),
                    u.Email,
                    u.Username,
                    u.IsActive
                })
                .Take(take)
                .ToListAsync();

            var students = await _context.Users
                .AsNoTracking()
                .Where(u => u.RoleId == 3)
                .Where(u =>
                    (isNumeric && u.UserId == numericId) ||
                    EF.Functions.Like(u.FirstName, likeTerm) ||
                    (u.MiddleName != null && EF.Functions.Like(u.MiddleName, likeTerm)) ||
                    EF.Functions.Like(u.LastName, likeTerm) ||
                    EF.Functions.Like(u.Email, likeTerm) ||
                    EF.Functions.Like(u.Username, likeTerm) ||
                    (u.Phone != null && EF.Functions.Like(u.Phone, likeTerm)))
                .OrderByDescending(u => u.UserId)
                .Select(u => new
                {
                    u.UserId,
                    u.RoleId,
                    FullName = $"{u.FirstName} {u.LastName}".Trim(),
                    u.Email,
                    u.Username,
                    u.IsActive
                })
                .Take(take)
                .ToListAsync();

            var exams = await (
                from e in _context.Exams.AsNoTracking()
                join c in _context.Courses.AsNoTracking() on e.CourseId equals c.CourseId
                join t in _context.Users.AsNoTracking() on e.TeacherId equals t.UserId
                where
                    (isNumeric && e.ExamId == numericId) ||
                    EF.Functions.Like(e.Title, likeTerm) ||
                    EF.Functions.Like(e.Description, likeTerm) ||
                    EF.Functions.Like(c.Title, likeTerm) ||
                    EF.Functions.Like(t.Username, likeTerm) ||
                    EF.Functions.Like(t.FirstName, likeTerm) ||
                    EF.Functions.Like(t.LastName, likeTerm)
                orderby e.CreatedAt descending
                select new
                {
                    e.ExamId,
                    e.Title,
                    CourseName = c.Title,
                    TeacherName = $"{t.FirstName} {t.LastName}".Trim(),
                    e.StartAt
                })
                .Take(take)
                .ToListAsync();

            return Ok(new
            {
                query = term,
                courses,
                teachers,
                students,
                exams,
                totalMatches = courses.Count + teachers.Count + students.Count + exams.Count
            });
        }

        [HttpGet("suggestions")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetSuggestions([FromQuery] string? status = null)
        {
            var query = _context.Suggestions
                .AsNoTracking()
                .Include(s => s.Student)
                .Include(s => s.Teacher)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                var normalizedStatus = status.Trim().ToLower();
                query = query.Where(s => s.Status != null && s.Status.ToLower() == normalizedStatus);
            }

            var data = await query
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new
                {
                    s.Id,
                    s.Title,
                    s.Message,
                    s.Reply,
                    s.Status,
                    s.CreatedAt,
                    StudentName = s.Student != null ? s.Student.Username : "Student",
                    StudentId = s.StudentId,
                    TeacherName = s.Teacher != null ? s.Teacher.Username : "Teacher",
                    TeacherId = s.TeacherId
                })
                .ToListAsync();

            return Ok(data);
        }

        [HttpPost]
		[Authorize(Roles = "Admin")]

		public async Task<IActionResult> PublishCourse([FromBody] int courseId)
        {
            var course = await _adminServices.PublishCourse(courseId);
            if(course == null)
            {
                return NotFound("Course Is Not Found !!");
            }

            var publishedCourse = await _context.Courses
                .AsNoTracking()
                .Include(c => c.Teacher)
                .FirstOrDefaultAsync(c => c.CourseId == course.CourseId);

            if (publishedCourse?.Teacher != null && !string.IsNullOrWhiteSpace(publishedCourse.Teacher.Email))
            {
                var body = EmailTemplateBuilder.BuildCoursePublishedEmail(
                    GetDisplayName(publishedCourse.Teacher),
                    publishedCourse.Title,
                    publishedCourse.StartDate,
                    publishedCourse.EndDate,
                    Convert.ToDecimal(publishedCourse.Fees),
                    $"{GetFrontendBaseUrl()}/teacher-dashboard/your-course");

                try
                {
                    await _emailSender.SendEmailAsync(
                        publishedCourse.Teacher.Email,
                        $"Course Published: {publishedCourse.Title}",
                        body,
                        isBodyHtml: true);
                }
                catch
                {
                    // Course publish should remain successful even if email delivery fails.
                }
            }

            await _activityEmailService.NotifyAdminsAsync(
                subject: $"Activity: Course Published - {course.Title}",
                activityTitle: "Course Published",
                summary: "A course was approved and published by admin.",
                detailsRows: new Dictionary<string, string>
                {
                    ["Course"] = course.Title,
                    ["Teacher ID"] = course.TeacherId.ToString(),
                    ["Published At"] = DateTime.UtcNow.ToString("dd MMM yyyy, hh:mm tt")
                },
                actionPathOrUrl: "/admin-dashboard/course-manage");

            return Ok(new
            {
                message = "Course Was Published"
            });
        }

		[HttpPost("courses/{courseId}/delete")]
		[Authorize(Roles = "Admin")]

		public async Task<IActionResult> DeleteCourseWiseId(int courseId)
		{
			var course = await _context.Courses
                .Include(c => c.Teacher)
				.FirstOrDefaultAsync(r => r.CourseId == courseId);

			if (course == null)
			{
				return NotFound(new { message = "Course not found!" });
			}

			var hasOrders = await _context.Orders
				.AnyAsync(o => o.CourseId == courseId);

			if (hasOrders)
			{
				return BadRequest(new
				{
					message = "Cannot delete course because orders exist for this course."
				});
			}

			_context.Courses.Remove(course);
			await _context.SaveChangesAsync();

            if (course.Teacher != null && !string.IsNullOrWhiteSpace(course.Teacher.Email))
            {
                var body = EmailTemplateBuilder.BuildAdminActivityEmail(
                    activityTitle: $"Hi {GetDisplayName(course.Teacher)}, a course was removed",
                    summary: "An administrator removed one of your courses from ExamNest.",
                    detailsRows: new Dictionary<string, string>
                    {
                        ["Course"] = course.Title,
                        ["Course ID"] = course.CourseId.ToString()
                    },
                    actionUrl: $"{GetFrontendBaseUrl()}/teacher-dashboard/your-course");

                try
                {
                    await _emailSender.SendEmailAsync(
                        course.Teacher.Email,
                        $"Course Removed: {course.Title}",
                        body,
                        isBodyHtml: true);
                }
                catch
                {
                    // Keep delete operation successful if email fails.
                }
            }

            await NotifyAdminsSafeAsync(
                subject: $"Activity: Course Deleted - {course.Title}",
                activityTitle: "Course Deleted by Admin",
                summary: "An admin deleted a course.",
                detailsRows: new Dictionary<string, string>
                {
                    ["Course"] = course.Title,
                    ["Course ID"] = course.CourseId.ToString(),
                    ["Teacher"] = course.Teacher != null ? GetDisplayName(course.Teacher) : $"Teacher #{course.TeacherId}"
                },
                actionPathOrUrl: "/admin-dashboard/course-manage");

			return Ok(new
			{
				message = "Course deleted successfully!"
			});
		}

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetLiveClasses([FromQuery] int? courseId = null)
        {
			var query = _context.LiveClassSchedules
                .AsNoTracking()
                .Include(x => x.Course)
                .Include(x => x.Teacher)
				.AsQueryable();

            if (courseId.HasValue && courseId.Value > 0)
            {
                query = query.Where(x => x.CourseId == courseId.Value);
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

		[HttpPost]
		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> CreateLiveClass([FromBody] LiveClassCreateDto dto)
		{
			if (dto == null)
				return BadRequest("Live class payload is required.");

			if (!TryGetCurrentUserId(out var adminId))
				return Unauthorized("Invalid admin token.");

			var title = (dto.Title ?? string.Empty).Trim();
			if (title.Length < 3 || title.Length > 150)
				return BadRequest("Title must be between 3 and 150 characters.");

			var meetingLink = (dto.MeetingLink ?? string.Empty).Trim();
			if (!Uri.TryCreate(meetingLink, UriKind.Absolute, out var meetingUri) ||
				(meetingUri.Scheme != Uri.UriSchemeHttp && meetingUri.Scheme != Uri.UriSchemeHttps))
			{
				return BadRequest("Meeting link must be a valid http/https URL.");
			}

			// ✅ Validate time
			if (dto.StartAt >= dto.EndAt)
				return BadRequest("Live class start time must be before end time.");

			var course = await _context.Courses
				.AsNoTracking()
				.FirstOrDefaultAsync(c => c.CourseId == dto.CourseId);

			if (course == null)
				return NotFound("Course not found.");

			if (!course.IsPublished)
				return BadRequest("Live class can be scheduled only for published courses.");

			if (dto.StartAt.Date < course.StartDate.Date || dto.EndAt.Date > course.EndDate.Date)
				return BadRequest("Live class must be within the selected course duration.");

			// ✅ Create entity (UTC safe)
			var liveClass = new LiveClassSchedule
			{
				CourseId = course.CourseId,
				TeacherId = course.TeacherId,
				ScheduledByAdminId = adminId,
				Title = title,
				Agenda = string.IsNullOrWhiteSpace(dto.Agenda) ? null : dto.Agenda.Trim(),
				MeetingLink = meetingLink,

				// ✅ Proper UTC handling
				StartAt = dto.StartAt.UtcDateTime,
				EndAt = dto.EndAt.UtcDateTime,
				CreatedAt = DateTime.UtcNow
			};

			_context.LiveClassSchedules.Add(liveClass);
			await _context.SaveChangesAsync();

            await NotifyLiveClassScheduledAsync(course, liveClass);

            await NotifyAdminsSafeAsync(
                subject: $"Activity: Live Class Scheduled - {liveClass.Title}",
                activityTitle: "Live Class Scheduled",
                summary: "An admin scheduled a new live class.",
                detailsRows: new Dictionary<string, string>
                {
                    ["Course"] = course.Title,
                    ["Live Class"] = liveClass.Title,
                    ["Teacher ID"] = liveClass.TeacherId.ToString(),
                    ["Starts"] = liveClass.StartAt.ToString("dd MMM yyyy, hh:mm tt"),
                    ["Ends"] = liveClass.EndAt.ToString("dd MMM yyyy, hh:mm tt")
                },
                actionPathOrUrl: "/admin-dashboard/live-classes");

			return Ok(new
			{
				message = "Live class scheduled successfully.",
				liveClass = new
				{
					liveClass.LiveClassScheduleId,
					liveClass.CourseId,
					CourseTitle = course.Title,
					liveClass.TeacherId,
					liveClass.Title,
					liveClass.Agenda,
					liveClass.MeetingLink,

					// ✅ Return in UTC (Angular will convert automatically)
					liveClass.StartAt,
					liveClass.EndAt,
					liveClass.IsCancelled,
					liveClass.CreatedAt
				}
			});
		}



		[HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteLiveClass(int id)
        {
            var liveClass = await _context.LiveClassSchedules
                .Include(x => x.Course)
                    .ThenInclude(c => c!.Teacher)
                .FirstOrDefaultAsync(x => x.LiveClassScheduleId == id);

            if (liveClass == null)
                return NotFound("Live class not found.");

            if (!string.IsNullOrWhiteSpace(liveClass.MaterialFilePath))
            {
                var rootPath = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
                var fullPath = Path.Combine(
                    rootPath,
                    liveClass.MaterialFilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

                if (System.IO.File.Exists(fullPath))
                    System.IO.File.Delete(fullPath);
            }

            _context.LiveClassSchedules.Remove(liveClass);
            await _context.SaveChangesAsync();

            await NotifyLiveClassCancelledAsync(liveClass);

            await NotifyAdminsSafeAsync(
                subject: $"Activity: Live Class Deleted - {liveClass.Title}",
                activityTitle: "Live Class Deleted",
                summary: "An admin deleted a scheduled live class.",
                detailsRows: new Dictionary<string, string>
                {
                    ["Live Class"] = liveClass.Title,
                    ["Course"] = liveClass.Course?.Title ?? $"Course #{liveClass.CourseId}",
                    ["Teacher"] = liveClass.Course?.Teacher != null
                        ? GetDisplayName(liveClass.Course.Teacher)
                        : $"Teacher #{liveClass.TeacherId}"
                },
                actionPathOrUrl: "/admin-dashboard/live-classes");

            return Ok(new { message = "Live class deleted successfully." });
        }


		// payment

		[HttpGet("payments")]
		[Authorize(Roles = "Admin")]

		public async Task<IActionResult> GetAllPayments()
		{
			var payments = await (
				from p in _context.Payments.AsNoTracking()
				join o in _context.Orders.AsNoTracking() on p.OrderId equals o.Id
				join c in _context.Courses.AsNoTracking() on o.CourseId equals c.CourseId
				join u in _context.Users.AsNoTracking() on o.StudentId equals u.UserId
				join s in _context.Subscriptions.AsNoTracking()
					on new { o.StudentId, o.CourseId }
					equals new { s.StudentId, s.CourseId } into sub
				from s in sub.DefaultIfEmpty()

				select new PaymentDetailDto
				{
					PaymentId = p.Id,
					RazorpayPaymentId = p.RazorpayPaymentId,
					Amount = p.Amount,
					Status = p.Status,
					PaymentDate = p.CreatedAt,

					CourseTitle = c.Title,
					CourseFees = c.Fees,

					StudentName = u.FirstName + " " + u.LastName,
					StudentEmail = u.Email,

					OrderId = o.OrderId,
					SubscriptionStatus = s != null ? s.Status : "N/A"
				}
			).ToListAsync();

			return Ok(payments);
		}

		[HttpGet("payments/{id}")]
		[Authorize(Roles = "Admin")]

		public async Task<IActionResult> GetPaymentDetail(int id)
		{
			var payment = await (
				from p in _context.Payments.AsNoTracking()
				join o in _context.Orders.AsNoTracking() on p.OrderId equals o.Id
				join c in _context.Courses.AsNoTracking() on o.CourseId equals c.CourseId
				join u in _context.Users.AsNoTracking() on o.StudentId equals u.UserId
				join s in _context.Subscriptions.AsNoTracking()
					on new { o.StudentId, o.CourseId }
					equals new { s.StudentId, s.CourseId } into sub
				from s in sub.DefaultIfEmpty()

				where p.Id == id

				select new PaymentDetailDto
				{
					PaymentId = p.Id,
					RazorpayPaymentId = p.RazorpayPaymentId,
					Amount = p.Amount,
					Status = p.Status,
					PaymentDate = p.CreatedAt,

					CourseTitle = c.Title,
					CourseFees = c.Fees,

					StudentName = u.FirstName + " " + u.LastName,
					StudentEmail = u.Email,

					OrderId = o.OrderId,
					SubscriptionStatus = s != null ? s.Status : "N/A"
				}
			).FirstOrDefaultAsync();

			if (payment == null)
				return NotFound(new { message = "Payment not found" });

			return Ok(payment);
		}



		[HttpGet("payments/check/{paymentId}")]
		[Authorize(Roles = "Admin")]

		public async Task<IActionResult> CheckPayment(string paymentId)
		{
			var payment = await _context.Payments
				.FirstOrDefaultAsync(p => p.RazorpayPaymentId == paymentId);

			if (payment == null)
			{
				return NotFound(new { message = "Payment not found" });
			}

			try
			{
				string key = _config["Razorpay:Key"]!;
				string secret = _config["Razorpay:SecretKey"]!;

				RazorpayClient client = new RazorpayClient(key, secret);

				Payment razorPayment = client.Payment.Fetch(paymentId);

				return Ok(new
				{
					razorpayPaymentId = paymentId,
					status = razorPayment["status"]?.ToString(),
					amount = razorPayment["amount"] != null
					   ? Convert.ToDecimal(razorPayment["amount"]) / 100
					   : 0,
					method = razorPayment["method"]?.ToString(),
					email = razorPayment["email"]?.ToString(),
					contact = razorPayment["contact"]?.ToString()
				});
			}
			catch (Exception ex)
			{
				return StatusCode(500, new
				{
					message = "Error checking payment",
					error = ex.Message
				});
			}
		}

		// exams 

		[HttpGet("exams")]
		[Authorize(Roles = "Admin")]

		public async Task<IActionResult> GetAllExams()
		{
			var exams = await _context.Exams
                .AsNoTracking()
				.Select(e => new
				{
					e.ExamId,
					e.Title,
					e.Description,
					e.CourseId,
					e.TeacherId,
					e.StartAt,
					e.EndAt,
					e.DurationMinutes,
					e.RandomQuestionCount,
					e.CreatedAt
				})
				.ToListAsync();

			return Ok(exams);
		}


		[HttpDelete("exams/{id}")]
		[Authorize(Roles = "Admin")]

		public async Task<IActionResult> DeleteExam(int id)
		{
			var exam = await _context.Exams
                .Include(e => e.Teacher)
                .Include(e => e.Course)
                .FirstOrDefaultAsync(e => e.ExamId == id);

			if (exam == null)
				return NotFound(new { message = "Exam not found" });

			// ❗ prevent delete if attempts exist
			//var hasAttempts = await _context.ExamAttempts
			//	.AnyAsync(a => a.ExamId == id);

			//if (hasAttempts)
			//	return BadRequest(new
			//	{
			//		message = "already attempted this exam"
			//	});

			_context.Exams.Remove(exam);
			await _context.SaveChangesAsync();

            if (exam.Teacher != null && !string.IsNullOrWhiteSpace(exam.Teacher.Email))
            {
                var body = EmailTemplateBuilder.BuildAdminActivityEmail(
                    activityTitle: $"Hi {GetDisplayName(exam.Teacher)}, an exam was removed",
                    summary: "An administrator removed one of your exams.",
                    detailsRows: new Dictionary<string, string>
                    {
                        ["Exam"] = exam.Title,
                        ["Course"] = exam.Course?.Title ?? $"Course #{exam.CourseId}"
                    },
                    actionUrl: $"{GetFrontendBaseUrl()}/teacher-dashboard/exam-list");

                try
                {
                    await _emailSender.SendEmailAsync(
                        exam.Teacher.Email,
                        $"Exam Deleted: {exam.Title}",
                        body,
                        isBodyHtml: true);
                }
                catch
                {
                    // Keep delete operation successful if email fails.
                }
            }

            await NotifyAdminsSafeAsync(
                subject: $"Activity: Exam Deleted - {exam.Title}",
                activityTitle: "Exam Deleted by Admin",
                summary: "An admin deleted an exam.",
                detailsRows: new Dictionary<string, string>
                {
                    ["Exam"] = exam.Title,
                    ["Exam ID"] = exam.ExamId.ToString(),
                    ["Teacher"] = exam.Teacher != null ? GetDisplayName(exam.Teacher) : $"Teacher #{exam.TeacherId}"
                },
                actionPathOrUrl: "/admin-dashboard/exams-manage");

			return Ok(new { message = "Exam deleted successfully" });
		}

		[HttpGet("exams/{examId}/students")]
		[Authorize(Roles = "Admin")]

		public async Task<IActionResult> GetStudentsByExam(int examId)
		{
			var students = await (
				from attempt in _context.ExamAttempts.AsNoTracking()
				join user in _context.Users.AsNoTracking()
					on attempt.StudentId equals user.UserId

				where attempt.ExamId == examId

				select new ExamStudentDto
				{
					UserId = user.UserId,
					Name = user.FirstName + " " + user.LastName,
					Email = user.Email,
					MaxScore = attempt.MaxScore,
					Score = attempt.TotalScore,
					Status = attempt.Status
				}
			).ToListAsync();

			return Ok(students);
		}

		// role side
		[HttpGet]
		[Authorize(Roles = "Admin")]

		public async Task<IActionResult> GetRoles()
		{
			return Ok(await _context.Roles.AsNoTracking().ToListAsync());
		}

		// GET by ID
		[HttpGet("{id}")]
		[Authorize(Roles = "Admin")]

		public async Task<IActionResult> GetRole(int id)
		{
			var role = await _context.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.RoleId == id);
			if (role == null) return NotFound();
			return Ok(role);
		}

		// CREATE
		[HttpPost]
		[Authorize(Roles = "Admin")]

		public async Task<IActionResult> CreateRole(Role role)
		{
			role.CreatedAt = DateTime.Now;
			_context.Roles.Add(role);
			await _context.SaveChangesAsync();

            await NotifyAdminsSafeAsync(
                subject: $"Activity: Role Created - {role.RoleName}",
                activityTitle: "Role Created",
                summary: "A new role was created by admin.",
                detailsRows: new Dictionary<string, string>
                {
                    ["Role"] = role.RoleName,
                    ["Role ID"] = role.RoleId.ToString()
                },
                actionPathOrUrl: "/admin-dashboard/role-manage");

			return Ok(role);
		}

		// UPDATE
		[HttpPut("{id}")]
		[Authorize(Roles = "Admin")]

		public async Task<IActionResult> UpdateRole(int id, Role role)
		{
			if (id != role.RoleId) return BadRequest();

			_context.Entry(role).State = EntityState.Modified;
			await _context.SaveChangesAsync();

            await NotifyAdminsSafeAsync(
                subject: $"Activity: Role Updated - {role.RoleName}",
                activityTitle: "Role Updated",
                summary: "An admin updated a role.",
                detailsRows: new Dictionary<string, string>
                {
                    ["Role"] = role.RoleName,
                    ["Role ID"] = role.RoleId.ToString()
                },
                actionPathOrUrl: "/admin-dashboard/role-manage");

			return Ok(role);
		}

		// DELETE
		[HttpDelete("{id}")]
		[Authorize(Roles = "Admin")]

		public async Task<IActionResult> DeleteRole(int id)
		{
			var role = await _context.Roles.FindAsync(id);
			if (role == null) return NotFound();

			_context.Roles.Remove(role);
			await _context.SaveChangesAsync();

            await NotifyAdminsSafeAsync(
                subject: $"Activity: Role Deleted - {role.RoleName}",
                activityTitle: "Role Deleted",
                summary: "An admin deleted a role.",
                detailsRows: new Dictionary<string, string>
                {
                    ["Role"] = role.RoleName,
                    ["Role ID"] = role.RoleId.ToString()
                },
                actionPathOrUrl: "/admin-dashboard/role-manage");

			return Ok();
		}


        // dashboard side
        [HttpGet("dashboarddata")]
		[Authorize(Roles = "Admin")]

		public async Task<IActionResult> GetDashboardData()
        {
            // Students Count
            var totalStudents = await _context.Users
                .Where(u => u.Role!.RoleName == "Student")
                .CountAsync();

            // Teachers Count
            var totalTeachers = await _context.Users
                .Where(u => u.Role!.RoleName == "Teacher")
                .CountAsync();

            // Exams Count
            var totalExams = await _context.Exams
                .CountAsync();

            // Attempts Status
            var submittedAttempts = await _context.ExamAttempts
                .Where(a => a.Status == "Submitted")
                .CountAsync();

            var pendingAttempts = await _context.ExamAttempts
                .Where(a => a.Status == "InProgress")
                .CountAsync();

            // Total Earnings (handle null safely)
            var totalEarnings = await _context.Payments
                .Where(p => p.Order!.Status == "Paid")
                .SumAsync(p => (decimal?)p.Amount) ?? 0;

            // total course
            var totalcourse = await _context.Courses.CountAsync();

            var publishedcourse = await _context.Courses
                .CountAsync(r => r.IsPublished);
            // Final Response
            return Ok(new
            {
                totalStudents = totalStudents,      // 0 if none
                totalTeachers = totalTeachers,     // 0 if none
                totalExams = totalExams,           // 0 if none
                submittedAttempts = submittedAttempts,
                pendingAttempts = pendingAttempts,
                totalEarnings = totalEarnings,
				totalcourse = totalcourse,
				publishedcourse = publishedcourse
            });
        }


        [HttpGet("top-exams")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetTopExams()
        {
            try
            {
                // 🔹 Step 1: Get latest 3 exams
                var exams = await (
                    from e in _context.Exams.AsNoTracking()
                    join c in _context.Courses.AsNoTracking() on e.CourseId equals c.CourseId
                    join u in _context.Users.AsNoTracking() on e.TeacherId equals u.UserId
                    orderby e.CreatedAt descending
                    select new
                    {
                        e.ExamId,
                        ExamTitle = e.Title,
                        CourseName = c.Title,
                        CreatedDate = e.CreatedAt,
                        IsPublished = c.IsPublished,
                        TeacherName = u.FirstName + " " + u.LastName
                    })
                    .Take(3)
                    .ToListAsync();

                // 🔹 Step 2: Extract exam IDs
                var examIds = exams.Select(e => e.ExamId).ToList();

                // ✅ Safety check
                if (!examIds.Any())
                    return Ok(new List<object>());

                // 🔹 Step 3: Calculate attempt percentages
                var attemptPercentages = await (
                    from a in _context.ExamAttemptAnswers.AsNoTracking()
                    join ea in _context.ExamAttempts on a.ExamAttemptId equals ea.ExamAttemptId
                    where examIds.Contains(ea.ExamId)
                    group new { a, ea } by new { ea.ExamId, a.ExamAttemptId } into g
                    select new
                    {
                        g.Key.ExamId,

                        // ✅ Safe calculation
                        Percentage = g.Count() == 0
                            ? 0
                            : (g.Sum(x => (double)x.a.MarksAwarded) * 100.0) / g.Count()
                    })
                    .ToListAsync();

                // 🔹 Step 4: Average per exam
                var avgByExam = attemptPercentages
                    .GroupBy(x => x.ExamId)
                    .ToDictionary(
                        g => g.Key,
                        g => Math.Round(g.Average(x => x.Percentage), 2)
                    );

                // 🔹 Step 5: Final result
                var result = exams.Select(e => new
                {
                    e.ExamTitle,
                    e.CourseName,
                    e.CreatedDate,
                    e.IsPublished,
                    e.TeacherName,
                    AvgPercentage = avgByExam.TryGetValue(e.ExamId, out var avg) ? avg : 0
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                // 🔥 Debug friendly
                return BadRequest(new
                {
                    message = ex.Message,
                    stack = ex.StackTrace
                });
            }
        }


        [HttpGet("CourseById/{CourseId}")]
		[Authorize(Roles = "Admin")]

		public async Task<IActionResult> GetCourseByid(int CourseId)
        {
            var course = await _context.Courses
                .AsNoTracking()
                .Include(r => r.Teacher)
				.Include(r=>r.CourseMedias)
                .Where(r => r.CourseId == CourseId)
                .Select(r => new
                {
                    CourseId = r.CourseId,
                    Title = r.Title,
                    Description = r.Description,
                    StartDate = r.StartDate,
                    EndDate = r.EndDate,
                    IsPublished = r.IsPublished,
                    Fees = r.Fees,
                    ThumbnailUrl = r.ThumbailUrl,
                    TeacherId = r.TeacherId,
                    TeacherName = r.Teacher!.FirstName + " " +r.Teacher.LastName, // 👈 extra useful
                    CreatedAt = r.CreatedAt,
                    Videos = r.CourseMedias!.Select(m => new
                    {
                        CourseMediaId = m.CourseMediaId,
                        FileName = m.FileName,
                        FilePath = m.FilePath,
                        FileType = m.FileType
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (course == null)
            {
                return NotFound("Course Not Found!");
            }

            return Ok(course);
        }

        [HttpGet("MyProfile")]
		[Authorize(Roles = "Teacher,Admin,Student")]
        public async Task<IActionResult> GetUserProfile()
        {
            // extract user id from JWT 'sub'
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? User.FindFirst("sub")?.Value;

            if (!int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized("Invalid token: sub claim missing or not integer");
            }

            var user = await _context.Users
                .AsNoTracking()
                .Where(u => u.UserId == userId)
                .Select(u => new UserProfileDto
                {
                    UserId = u.UserId,
                    FirstName = u.FirstName,
                    MiddleName = u.MiddleName,
                    LastName = u.LastName,
                    Email = u.Email,
                    Username = u.Username,
                    Phone = u.Phone,
                    ProfileImageUrl = u.ProfileImageUrl,
                    RoleId = u.RoleId,
                    IsActive = u.IsActive,
                    LastLoginAt = u.LastLoginAt,
                    CreatedAt = u.CreatedAt,
                    UpdatedAt = u.UpdatedAt
                })
                .FirstOrDefaultAsync();

            if (user == null) return NotFound("User not found!");

            return Ok(user);
        }

		[HttpPost("profile/update")]
		[Authorize(Roles = "Teacher,Admin,Student")]
		public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto model)
		{
			// 🔐 Get userId from JWT
			var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
							  ?? User.FindFirst("sub")?.Value;

			if (!int.TryParse(userIdClaim, out int userId))
			{
				return Unauthorized("Invalid token");
			}

			var user = await _context.Users.FindAsync(userId);

			if (user == null)
				return NotFound("User not found");

			// ✅ Update fields
			user.FirstName = model.FirstName;
			user.MiddleName = model.MiddleName;
			user.LastName = model.LastName;
			user.Email = model.Email;
			user.Phone = model.Phone;

			user.UpdatedAt = DateTime.UtcNow;

			await _context.SaveChangesAsync();

            await NotifyAdminsSafeAsync(
                subject: $"Activity: Profile Updated - {GetDisplayName(user)}",
                activityTitle: "User Profile Updated",
                summary: "A user updated their profile details.",
                detailsRows: new Dictionary<string, string>
                {
                    ["User"] = GetDisplayName(user),
                    ["User ID"] = user.UserId.ToString(),
                    ["Role ID"] = user.RoleId.ToString()
                },
                actionPathOrUrl: "/admin-dashboard/profile");

			return Ok(new
			{
				success = true,
				message = "Profile updated successfully"
			});
		}

        [HttpPost("profile/upload-image")]
        [Authorize(Roles = "Teacher,Admin,Student")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(MaxProfileImageSizeBytes)]
        public async Task<IActionResult> UploadProfileImage([FromForm] UpdateProfileImageDto model)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("sub")?.Value;

            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized("Invalid token");

            if (model.ProfileImage == null || model.ProfileImage.Length <= 0)
                return BadRequest("Profile image is required.");

            if (model.ProfileImage.Length > MaxProfileImageSizeBytes)
                return BadRequest("Profile image must be at most 20 MB.");

            var extension = Path.GetExtension(model.ProfileImage.FileName);
            if (!AllowedProfileImageExtensions.Contains(extension))
                return BadRequest("Profile image must be jpg, jpeg, png, or webp.");

            if (!string.IsNullOrWhiteSpace(model.ProfileImage.ContentType) &&
                !AllowedProfileImageContentTypes.Contains(model.ProfileImage.ContentType))
            {
                return BadRequest("Profile image content type is invalid.");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
                return NotFound("User not found.");

            var rootPath = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var profileFolder = Path.Combine(rootPath, "ProfileImages");

            if (!Directory.Exists(profileFolder))
                Directory.CreateDirectory(profileFolder);

            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(profileFolder, fileName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await model.ProfileImage.CopyToAsync(stream, HttpContext.RequestAborted);
            }

            var previousImageUrl = user.ProfileImageUrl;
            user.ProfileImageUrl = $"/ProfileImages/{fileName}";
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(previousImageUrl) &&
                previousImageUrl.StartsWith("/ProfileImages/", StringComparison.OrdinalIgnoreCase))
            {
                var previousPath = Path.Combine(rootPath, previousImageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(previousPath))
                    System.IO.File.Delete(previousPath);
            }

            await NotifyAdminsSafeAsync(
                subject: $"Activity: Profile Image Updated - {GetDisplayName(user)}",
                activityTitle: "Profile Image Updated",
                summary: "A user uploaded a new profile image.",
                detailsRows: new Dictionary<string, string>
                {
                    ["User"] = GetDisplayName(user),
                    ["User ID"] = user.UserId.ToString(),
                    ["Role ID"] = user.RoleId.ToString()
                },
                actionPathOrUrl: "/admin-dashboard/profile");

            return Ok(new
            {
                success = true,
                message = "Profile image updated successfully",
                profileImageUrl = user.ProfileImageUrl
            });
        }

        private async Task NotifyLiveClassScheduledAsync(Course course, LiveClassSchedule liveClass)
        {
            await NotifyTeacherAboutLiveClassAsync(course, liveClass, isCancellation: false);
            await NotifyStudentsAboutLiveClassAsync(course, liveClass, isCancellation: false);
        }

        private async Task NotifyLiveClassCancelledAsync(LiveClassSchedule liveClass)
        {
            var course = liveClass.Course ?? await _context.Courses
                .AsNoTracking()
                .Include(c => c.Teacher)
                .FirstOrDefaultAsync(c => c.CourseId == liveClass.CourseId);

            if (course == null)
                return;

            await NotifyTeacherAboutLiveClassAsync(course, liveClass, isCancellation: true);
            await NotifyStudentsAboutLiveClassAsync(course, liveClass, isCancellation: true);
        }


		[HttpGet("course-earnings")]
		public async Task<IActionResult> GetCourseEarnings()
		{
			var result = await (
				from c in _context.Courses
				join o in _context.Orders on c.CourseId equals o.CourseId
				join p in _context.Payments on o.Id equals p.OrderId
				where p.Status == "Success"
				group p by new { c.CourseId, c.Title } into g
				select new
				{
					CourseId = g.Key.CourseId,
					Title = g.Key.Title,
					TotalEarning = g.Sum(x => x.Amount)
				}
			).ToListAsync();

			return Ok(result);
		}


		private async Task NotifyTeacherAboutLiveClassAsync(Course course, LiveClassSchedule liveClass, bool isCancellation)
        {
            var teacher = course.Teacher ?? await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == liveClass.TeacherId);

            if (teacher == null || string.IsNullOrWhiteSpace(teacher.Email))
                return;

            var teacherUrl = $"{GetFrontendBaseUrl()}/teacher-dashboard/live-classes";
            var body = isCancellation
                ? EmailTemplateBuilder.BuildLiveClassCancelledEmail(
                    GetDisplayName(teacher),
                    course.Title,
                    liveClass.Title,
                    liveClass.StartAt,
                    liveClass.EndAt,
                    teacherUrl)
                : EmailTemplateBuilder.BuildLiveClassScheduledEmail(
                    GetDisplayName(teacher),
                    course.Title,
                    liveClass.Title,
                    liveClass.StartAt,
                    liveClass.EndAt,
                    liveClass.MeetingLink,
                    liveClass.Agenda,
                    teacherUrl);

            var subject = isCancellation
                ? $"Live Class Cancelled: {liveClass.Title}"
                : $"Live Class Scheduled: {liveClass.Title}";

            try
            {
                await _emailSender.SendEmailAsync(
                    teacher.Email,
                    subject,
                    body,
                    isBodyHtml: true);
            }
            catch
            {
                // Keep live class operation successful if teacher email fails.
            }
        }

        private async Task NotifyStudentsAboutLiveClassAsync(Course course, LiveClassSchedule liveClass, bool isCancellation)
        {
            var students = await _context.Subscriptions
                .AsNoTracking()
                .Where(s => s.CourseId == course.CourseId && s.Status == "Active")
                .Select(s => s.Student)
                .Where(s => s != null && !string.IsNullOrWhiteSpace(s.Email))
                .Distinct()
                .ToListAsync();

            if (students.Count == 0)
                return;

            var studentUrl = $"{GetFrontendBaseUrl()}/student-dashboard/live-classes";

            foreach (var student in students)
            {
                if (student == null || string.IsNullOrWhiteSpace(student.Email))
                    continue;

                var body = isCancellation
                    ? EmailTemplateBuilder.BuildLiveClassCancelledEmail(
                        GetDisplayName(student),
                        course.Title,
                        liveClass.Title,
                        liveClass.StartAt,
                        liveClass.EndAt,
                        studentUrl)
                    : EmailTemplateBuilder.BuildLiveClassScheduledEmail(
                        GetDisplayName(student),
                        course.Title,
                        liveClass.Title,
                        liveClass.StartAt,
                        liveClass.EndAt,
                        liveClass.MeetingLink,
                        liveClass.Agenda,
                        studentUrl);

                var subject = isCancellation
                    ? $"Live Class Cancelled: {liveClass.Title}"
                    : $"Live Class Scheduled: {liveClass.Title}";

                try
                {
                    await _emailSender.SendEmailAsync(
                        student.Email,
                        subject,
                        body,
                        isBodyHtml: true);
                }
                catch
                {
                    // Continue notifying other students if one email fails.
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

        private bool TryGetCurrentUserId(out int userId)
        {
            userId = 0;
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                              ?? User.FindFirstValue("sub");

            return int.TryParse(userIdClaim, out userId);
        }

        private string GetFrontendBaseUrl()
        {
            var configured = _config["Frontend:BaseUrl"];
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
	}
}
