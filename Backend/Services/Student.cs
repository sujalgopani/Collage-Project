using ExamNest.Data;
using ExamNest.Models;
using Microsoft.EntityFrameworkCore;

namespace ExamNest.Services
{
    public class Student
    {
        private readonly AppDbContext _context;
        public Student(AppDbContext context) {
            _context = context;
        }

        public async Task<List<object>> GetCourses(int studentId, string? search = null)
        {
            var subscribedCourseIds = _context.Subscriptions
                .AsNoTracking()
                .Where(s => s.StudentId == studentId)
                .Select(s => s.CourseId);

            var query = _context.Courses
                .AsNoTracking()
                .AsQueryable();

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
                .Select(c => new
                {
                    c.CourseId,
                    c.Title,
                    c.Fees,
                    c.ThumbailUrl,
                    c.StartDate,
                    c.EndDate,
                    IsSubscribed = subscribedCourseIds.Contains(c.CourseId),
                    Ispublished = c.IsPublished
                })
                .ToListAsync();
            return courses.Cast<object>().ToList();
        }

        public async Task<Course?> GetCourseById(int courseId)
        {
			return await _context.Courses
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CourseId == courseId);
		}
        
    }
}
