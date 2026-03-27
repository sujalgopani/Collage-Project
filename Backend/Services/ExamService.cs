using ExamNest.Data;
using ExamNest.Models;
using ExamNest.Models.DTOs.Exam;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;


namespace ExamNest.Services
{
    public enum PublishResultOutcome
    {
        Published = 0,
        ExamNotFound = 1,
        NoSubmittedAttempts = 2,
        AlreadyPublished = 3
    }

    public class ExamService
    {
        private readonly AppDbContext _context;
        public ExamService(AppDbContext context) {
            _context = context;
        }

		public async Task<List<ExamlistDTO>> GetTeacherWiseCourse(int teacherId)
		{
			var data = await _context.Exams
                .AsNoTracking()
				.Where(r => r.TeacherId == teacherId)
				.Select(r => new ExamlistDTO
				{
					ExamId = r.ExamId,
					CourseId = r.CourseId,
					tittle = r.Title,   // consider renaming to 'Title' in DTO
					TeacherName = r.Teacher != null ? r.Teacher.Username : string.Empty,
					Description = r.Description,
					DurationMinit = r.DurationMinutes,
					CreatedAt = r.CreatedAt,
					Enddate = r.EndAt,
					Startdate = r.StartAt,
					Isflagged = r.IsResultPublished
				})
				.ToListAsync();

			return data;
		}

		public async Task<PublishResultOutcome> PublishExamResult(int examId)
		{
            var exam = await _context.Exams
                .FirstOrDefaultAsync(x => x.ExamId == examId);

            if (exam == null)
                return PublishResultOutcome.ExamNotFound;

            if (exam.IsResultPublished)
                return PublishResultOutcome.AlreadyPublished;

			var hasSubmittedAttempts = await _context.ExamAttempts
                .AnyAsync(x => x.ExamId == examId && x.SubmittedAt != null);

			if (!hasSubmittedAttempts)
				return PublishResultOutcome.NoSubmittedAttempts;

            exam.IsResultPublished = true;
            exam.ResultPublishedAt = DateTime.UtcNow;

			await _context.SaveChangesAsync();

			return PublishResultOutcome.Published;
		}



	}
}
