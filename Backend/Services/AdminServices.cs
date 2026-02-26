using ExamNest.Data;
using ExamNest.Models;
using ExamNest.Models.DTOs.User;
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
        public async Task<List<User>> GetAllAsync()
        {
            return await _context.Users
                .Where(u => u.RoleId == 2)
                .ToListAsync();
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == id && u.RoleId == 2);
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

        public async Task<bool> UpdateAsync(int id, UserUpdateDTO dto)
        {
            var teacher = await GetByIdAsync(id);
            if (teacher == null) return false;

            var newhaspsw = BCrypt.Net.BCrypt.HashPassword(dto.Newpassword);
            teacher.FirstName = dto.FirstName;
            teacher.MiddleName = dto.MiddleName;
            teacher.LastName = dto.LastName;
            teacher.Email = dto.Email;
            teacher.PasswordHash = newhaspsw;
            teacher.Phone = dto.Phone;
            teacher.IsActive = dto.IsActive;
            teacher.UpdatedAt = DateTime.UtcNow;

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












    }
}
