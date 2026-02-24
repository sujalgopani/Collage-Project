using Microsoft.EntityFrameworkCore;
using ExamNest.Models;

namespace ExamNest.Data
{
    public class AppDbContext : DbContext

    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Role> Roles { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserGoogleAuth> UserGoogleAuths { get; set; }
        public DbSet<EmailOtp> EmailOtps { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Roles
            modelBuilder.Entity<Role>()
                .HasIndex(r => r.RoleName)
                .IsUnique();

            modelBuilder.Entity<Role>()
                .Property(r => r.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            // Users
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<User>()
                .Property(u => u.IsActive)
                .HasDefaultValue(false);

            modelBuilder.Entity<User>()
                .Property(u => u.FailedLoginAttempts)
                .HasDefaultValue(0);

            modelBuilder.Entity<User>()
                .Property(u => u.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<User>()
                .Property(u => u.UpdatedAt)
                .HasDefaultValueSql("GETDATE()");

            // UserGoogleAuth
            modelBuilder.Entity<UserGoogleAuth>()
                .HasIndex(g => g.GoogleSub)
                .IsUnique();

            modelBuilder.Entity<UserGoogleAuth>()
                .HasIndex(g => g.GoogleEmail)
                .IsUnique();

            modelBuilder.Entity<UserGoogleAuth>()
                .Property(g => g.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            // EmailOtp
            modelBuilder.Entity<EmailOtp>()
                .Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<EmailOtp>()
                .Property(e => e.IsUsed)
                .HasDefaultValue(false);

            // Relationships
            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany()
                .HasForeignKey(u => u.RoleId);

            modelBuilder.Entity<UserGoogleAuth>()
                .HasOne(g => g.User)
                .WithMany()
                .HasForeignKey(g => g.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<EmailOtp>()
                .HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Role>().HasData(
                new Role { RoleId = 1, RoleName = "Admin", CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Role { RoleId = 2, RoleName = "Teacher", CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Role { RoleId = 3, RoleName = "Student", CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
            );
        }
    }
}
