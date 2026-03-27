using ExamNest.Data;
using ExamNest.Models;
using ExamNest.Models.DTOs.User;
using ExamNest.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Backend.Tests;

public class AdminServicesTests
{
    [Fact]
    public async Task UpdateAsync_BlankNewPassword_KeepsExistingPasswordHash()
    {
        await using var context = BuildContext();
        var user = await SeedTeacherAsync(context, "teacher.blank@example.com", "OldPass123!");
        var originalHash = user.PasswordHash;
        var service = new AdminServices(context);

        var updated = await service.UpdateAsync(user.UserId, new UserUpdateDTO
        {
            FirstName = "Updated",
            MiddleName = "T",
            LastName = "Teacher",
            Email = user.Email,
            Newpassword = "",
            Phone = "9876543210",
            IsActive = true
        });

        Assert.True(updated);

        var refreshed = await context.Users.AsNoTracking().FirstAsync(u => u.UserId == user.UserId);
        Assert.Equal(originalHash, refreshed.PasswordHash);
    }

    [Fact]
    public async Task UpdateAsync_WithNewPassword_UpdatesPasswordHash()
    {
        await using var context = BuildContext();
        var user = await SeedTeacherAsync(context, "teacher.new@example.com", "OldPass123!");
        var originalHash = user.PasswordHash;
        var service = new AdminServices(context);

        var updated = await service.UpdateAsync(user.UserId, new UserUpdateDTO
        {
            FirstName = "Updated",
            MiddleName = "T",
            LastName = "Teacher",
            Email = user.Email,
            Newpassword = "NewPass456!",
            Phone = "9876543210",
            IsActive = true
        });

        Assert.True(updated);

        var refreshed = await context.Users.AsNoTracking().FirstAsync(u => u.UserId == user.UserId);
        Assert.NotEqual(originalHash, refreshed.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify("NewPass456!", refreshed.PasswordHash));
    }

    private static AppDbContext BuildContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static async Task<User> SeedTeacherAsync(AppDbContext context, string email, string password)
    {
        context.Users.Add(new User
        {
            FirstName = "Test",
            LastName = "Teacher",
            Email = email,
            Username = email.Split('@')[0],
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Phone = "9999999999",
            RoleId = 2,
            IsActive = true,
            FailedLoginAttempts = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();
        return await context.Users.AsNoTracking().FirstAsync(u => u.Email == email);
    }
}
