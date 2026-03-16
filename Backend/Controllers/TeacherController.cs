using System.Globalization;
using System.IO.Compression;
using System.Security.Claims;
using System.Xml.Linq;
using ExamNest.Data;
using ExamNest.Models;
using ExamNest.Models.DTOs;
using ExamNest.Models.DTOs.Exam;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.FileIO;
using Org.BouncyCastle.Bcpg;

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

            if (dto.ThumbailUrl != null)
            {
                var rootPath = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
                var folder = Path.Combine(rootPath, "CourseThumbnail");

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                var fileName = Guid.NewGuid() + Path.GetExtension(dto.ThumbailUrl!.FileName);
                var filePath = Path.Combine(folder, fileName);

                await using (var stream = new FileStream(filePath, FileMode.Create))
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

                    await using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    _context.CourseMedias.Add(new CourseMedia
                    {
                        CourseId = course.CourseId,
                        FilePath = "/CourseVideos/" + fileName
                    });
                }

                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Course Created Successfully" });
        }

        [HttpGet("mycourses")]
        [Authorize(Roles = "Teacher")]
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

        [HttpPost("upload-exam-excel")]
        [Authorize(Roles = "Teacher")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadExamFromExcel([FromForm] ExamUploadFromExcelDto dto)
        {
            if (dto.ExcelFile == null || dto.ExcelFile.Length == 0)
                return BadRequest("Excel file is required.");

            if (dto.StartAt >= dto.EndAt)
                return BadRequest("Exam start time must be before end time.");

            if (dto.DurationMinutes <= 0)
                return BadRequest("DurationMinutes must be greater than 0.");

            var teacherIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(teacherIdClaim, out var teacherId))
                return Unauthorized("Invalid teacher token.");

            var ownedCourse = await _context.Courses
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

            return Ok(new
            {
                message = "Exam created successfully from Excel.",
                examId = exam.ExamId,
                totalQuestions = questions.Count,
                questionsPerStudent = finalQuestionCount
            });
        }


		// DASHBOARD SIDE 

		[HttpGet("GetTotalCourses")]
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
