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
				.Select(r => new ExamlistDTO
				{
					ExamId = r.ExamId,
					CourseId = r.CourseId,
					tittle = r.Title,   // consider renaming to 'Title' in DTO
					TeacherName = r.Teacher!.Username,
					Description = r.Description,
					DurationMinit = r.DurationMinutes,
					CreatedAt = r.CreatedAt
				})
				.ToListAsync();

			return data;
		}



	}
}
