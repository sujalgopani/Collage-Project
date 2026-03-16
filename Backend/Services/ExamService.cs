using ExamNest.Data;
using ExamNest.Models;
using ExamNest.Models.DTOs.Exam;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;


namespace ExamNest.Services
{
    public class ExamService
    {
        private readonly AppDbContext _context;
        public ExamService(AppDbContext context) {
            _context = context;
        }

		public async Task<List<ExamlistDTO>> GetTeacherWiseCourse(int teacherId)
		{
			var data = await _context.Exams
				.Where(r => r.TeacherId == teacherId)
				.Include(r => r.Teacher)
				.Include(r => r.Course)
				.Include(r=>r.ExamAttempts)
				.Select(r => new ExamlistDTO
				{
					ExamId = r.ExamId,
					CourseId = r.CourseId,
					tittle = r.Title,   // consider renaming to 'Title' in DTO
					TeacherName = r.Teacher!.Username,
					Description = r.Description,
					DurationMinit = r.DurationMinutes,
					CreatedAt = r.CreatedAt,
					Enddate = r.EndAt,
					Startdate = r.StartAt,
					Isflagged = r.ExamAttempts!.Any(a => a.IsFlagged)
				})
				.ToListAsync();

			return data;
		}

		public async Task<bool> PublishExamResult(int examId)
		{
			var attempts = await _context.ExamAttempts
							.Where(x => x.ExamId == examId)
							.ToListAsync();

			if (attempts.Count == 0)
				return false;

			foreach (var item in attempts)
			{
				item.IsFlagged = true;
			}

			await _context.SaveChangesAsync();

			return true;
		}



	}
}
