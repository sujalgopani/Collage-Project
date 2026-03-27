using System.Runtime.InteropServices;
using ExamNest.Data;
using ExamNest.Models;
using ExamNest.Models.DTOs.User;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ExamNest.Services
{
    public class AdminServices
    {
        private readonly AppDbContext _context;

        public AdminServices(AppDbContext context)
        {
            _context = context;
        }


        // teacher side service
        public async Task<List<User>> GetAllTeacher(string? search = null)
        {
            var query = _context.Users
                .AsNoTracking()
                .Where(u => u.RoleId == 2);

            query = ApplyUserSearch(query, search);

            return await query
                .OrderByDescending(u => u.UserId)
                .ToListAsync();
        }

        public async Task<List<User>> GetAllStudent(string? search = null)
        {
            var query = _context.Users
                .AsNoTracking()
                .Where(u => u.RoleId == 3);

            query = ApplyUserSearch(query, search);

            return await query
                .OrderByDescending(u => u.UserId)
                .ToListAsync();
        }

        // teacher
        public async Task<User?> GetByIdAsync(int id)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == id && u.RoleId == 2);
        }

        // student
        public async Task<User?> GetByIdstuAsync(int id)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == id && u.RoleId == 3);
        }


        public async Task<User> CreateAsync(UserCreateDTO dto)
        {
            var haspsw = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            var teacher = new User
            {
                FirstName = dto.FirstName,
                MiddleName = dto.MiddleName,
                LastName = dto.LastName,
                Email = dto.Email,
                PasswordHash = haspsw, // later hash
                Phone = dto.Phone,
                RoleId = 2, // fixed teacher role
                IsActive = true,
                LastLoginAt = DateTime.Now,
                FailedLoginAttempts = 0,
                CreatedAt = DateTime.UtcNow,
            };

            _context.Users.Add(teacher);
            await _context.SaveChangesAsync();

            return teacher;
        }

        // teacher
        public async Task<bool> UpdateAsync(int id, UserUpdateDTO dto)
        {
            var teacher = await GetByIdAsync(id);
            if (teacher == null) return false;

            teacher.FirstName = dto.FirstName;
            teacher.MiddleName = dto.MiddleName;
            teacher.LastName = dto.LastName;
            teacher.Email = dto.Email;
            teacher.Phone = dto.Phone;
            teacher.IsActive = dto.IsActive;
            teacher.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(dto.Newpassword))
            {
                teacher.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Newpassword);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        // student
        public async Task<bool> UpdateStudentAsync(int id, UserUpdateDTO dto)
        {
            var teacher = await GetByIdstuAsync(id);
            if (teacher == null) return false;

            teacher.FirstName = dto.FirstName;
            teacher.MiddleName = dto.MiddleName;
            teacher.LastName = dto.LastName;
            teacher.Email = dto.Email;
            teacher.Phone = dto.Phone;
            teacher.IsActive = dto.IsActive;
            teacher.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(dto.Newpassword))
            {
                teacher.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Newpassword);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var teacher = await GetByIdAsync(id);
            if (teacher == null) return false;

            _context.Users.Remove(teacher);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteStudentAsync(int id)
        {
            var teacher = await GetByIdstuAsync(id);
            if (teacher == null) return false;

            _context.Users.Remove(teacher);
            await _context.SaveChangesAsync();
            return true;
        }

		// course

		public async Task<List<Course>> GetAllCourses()
		{
            return await _context.Courses
                .AsNoTracking()
                .Include(c => c.Teacher)
                .ToListAsync();
		}


        public async Task<Course?> PublishCourse(int courseid)
        {
            var data = await _context.Courses.FirstOrDefaultAsync(c => c.CourseId == courseid);
            if(data == null)
            {
                return null;
            }

            data.IsPublished = true;
            _context.Courses.Update(data);
            await _context.SaveChangesAsync();
            return data;

        }





        private static IQueryable<User> ApplyUserSearch(IQueryable<User> query, string? search)
        {
            if (string.IsNullOrWhiteSpace(search))
                return query;

            var term = search.Trim();
            var likeTerm = $"%{term}%";
            var isNumeric = int.TryParse(term, out var userId);

            return query.Where(u =>
                (isNumeric && u.UserId == userId) ||
                EF.Functions.Like(u.FirstName, likeTerm) ||
                (u.MiddleName != null && EF.Functions.Like(u.MiddleName, likeTerm)) ||
                EF.Functions.Like(u.LastName, likeTerm) ||
                EF.Functions.Like(u.Email, likeTerm) ||
                EF.Functions.Like(u.Username, likeTerm) ||
                (u.Phone != null && EF.Functions.Like(u.Phone, likeTerm))
            );
        }



	}
}
