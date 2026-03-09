using ExamNest.Data;
using Microsoft.EntityFrameworkCore;

namespace ExamNest.Services
{
    public class Student
    {
        private readonly AppDbContext _context;
        public Student(AppDbContext context) {
            _context = context;
        }

        public async Task<List<object>> GetCourses(int studentId)
        {
            var courses = await _context.Courses
                .Select(c => new
                {
                    c.CourseId,
                    c.Title,
                    c.Fees,
                    c.ThumbailUrl,
                    c.StartDate,
                    c.EndDate,
                    IsSubscribed = _context.Subscriptions
                        .Any(s => s.StudentId == studentId && s.CourseId == c.CourseId)
                })
                .ToListAsync();
            return courses.Cast<object>().ToList();
        }

    }
}
