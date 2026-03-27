using Microsoft.EntityFrameworkCore;
using ExamNest.Models;
using ExamNest.Models.Payment;

namespace ExamNest.Data
{
    public class AppDbContext : DbContext

    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Role> Roles { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserGoogleAuth> UserGoogleAuths { get; set; }
        public DbSet<EmailOtp> EmailOtps { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<CourseMedia> CourseMedias { get; set; }
        public DbSet<Exam> Exams { get; set; }
        public DbSet<ExamQuestion> ExamQuestions { get; set; }
        public DbSet<ExamAttempt> ExamAttempts { get; set; }
        public DbSet<ExamAttemptAnswer> ExamAttemptAnswers { get; set; }
        public DbSet<ExamViolationEvent> ExamViolationEvents { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<Suggestion> Suggestions { get; set; }
        public DbSet<LiveClassSchedule> LiveClassSchedules { get; set; }
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
                .HasIndex(u => u.Email);
                //.IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => new { u.RoleId, u.IsActive });

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

            modelBuilder.Entity<User>()
                .Property(u => u.ProfileImageUrl)
                .HasMaxLength(500);

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


            // Order -> Student
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Student)
                .WithMany()
                .HasForeignKey(o => o.StudentId)
                .OnDelete(DeleteBehavior.NoAction);

            // Order -> Course
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Course)
                .WithMany()
                .HasForeignKey(o => o.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            // Payment -> Order
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Order)
                .WithMany()
                .HasForeignKey(p => p.OrderId)
                .OnDelete(DeleteBehavior.NoAction);

            // Subscription -> Student
            modelBuilder.Entity<Subscription>()
                .HasOne(s => s.Student)
                .WithMany()
                .HasForeignKey(s => s.StudentId)
                .OnDelete(DeleteBehavior.NoAction);

            // Subscription -> Course
            modelBuilder.Entity<Subscription>()
                .HasOne(s => s.Course)
                .WithMany()
                .HasForeignKey(s => s.CourseId)
                .OnDelete(DeleteBehavior.NoAction);

            // Exam -> Course
            modelBuilder.Entity<Exam>()
                .HasOne(e => e.Course)
                .WithMany()
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            // Exam -> Teacher
            modelBuilder.Entity<Exam>()
                .HasOne(e => e.Teacher)
                .WithMany()
                .HasForeignKey(e => e.TeacherId)
                .OnDelete(DeleteBehavior.NoAction);

            // ExamQuestion -> Exam
            modelBuilder.Entity<ExamQuestion>()
                .HasOne(q => q.Exam)
                .WithMany(e => e.Questions)
                .HasForeignKey(q => q.ExamId)
                .OnDelete(DeleteBehavior.Cascade);

            // ExamAttempt -> Exam
            modelBuilder.Entity<ExamAttempt>()
                .HasOne(a => a.Exam)
                .WithMany()
                .HasForeignKey(a => a.ExamId)
                .OnDelete(DeleteBehavior.Cascade);

            // ExamAttempt -> Student
            modelBuilder.Entity<ExamAttempt>()
                .HasOne(a => a.Student)
                .WithMany()
                .HasForeignKey(a => a.StudentId)
                .OnDelete(DeleteBehavior.NoAction);

            // ExamAttemptAnswer -> ExamAttempt
            modelBuilder.Entity<ExamAttemptAnswer>()
                .HasOne(a => a.ExamAttempt)
                .WithMany(x => x.Answers)
                .HasForeignKey(a => a.ExamAttemptId)
                .OnDelete(DeleteBehavior.Cascade);

            // ExamAttemptAnswer -> ExamQuestion
            modelBuilder.Entity<ExamAttemptAnswer>()
                .HasOne(a => a.ExamQuestion)
                .WithMany()
                .HasForeignKey(a => a.ExamQuestionId)
                .OnDelete(DeleteBehavior.NoAction);

            // ExamViolationEvent -> ExamAttempt
            modelBuilder.Entity<ExamViolationEvent>()
                .HasOne(v => v.ExamAttempt)
                .WithMany(a => a.ViolationEvents)
                .HasForeignKey(v => v.ExamAttemptId)
                .OnDelete(DeleteBehavior.Cascade);

            // exam => examattemp
			modelBuilder.Entity<ExamAttempt>()
		        .HasOne(a => a.Exam)
		        .WithMany(e => e.ExamAttempts)
		        .HasForeignKey(a => a.ExamId)
		        .OnDelete(DeleteBehavior.Cascade);

			// Decimal Precision
			modelBuilder.Entity<Order>()
                .Property(o => o.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Payment>()
                .Property(p => p.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Subscription>()
                .HasIndex(s => new { s.StudentId, s.CourseId })
                .IsUnique();

            modelBuilder.Entity<Exam>()
                .HasIndex(e => new { e.CourseId, e.StartAt });

            modelBuilder.Entity<Exam>()
                .Property(e => e.IsResultPublished)
                .HasDefaultValue(false);

            modelBuilder.Entity<ExamAttempt>()
                .HasIndex(a => new { a.ExamId, a.StudentId });

            modelBuilder.Entity<ExamAttemptAnswer>()
                .HasIndex(a => new { a.ExamAttemptId, a.ExamQuestionId })
                .IsUnique();

            modelBuilder.Entity<Suggestion>()
               .HasOne(s => s.Student)
               .WithMany()
               .HasForeignKey(s => s.StudentId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Suggestion>()
                .HasOne(s => s.Teacher)
                .WithMany()
                .HasForeignKey(s => s.TeacherId)
                .OnDelete(DeleteBehavior.Restrict); // ✅ FIX

            modelBuilder.Entity<LiveClassSchedule>()
                .Property(x => x.Title)
                .HasMaxLength(150);

            modelBuilder.Entity<LiveClassSchedule>()
                .Property(x => x.Agenda)
                .HasMaxLength(1000);

            modelBuilder.Entity<LiveClassSchedule>()
                .Property(x => x.MeetingLink)
                .HasMaxLength(1000);

            modelBuilder.Entity<LiveClassSchedule>()
                .Property(x => x.MaterialTitle)
                .HasMaxLength(200);

            modelBuilder.Entity<LiveClassSchedule>()
                .Property(x => x.MaterialDescription)
                .HasMaxLength(1000);

            modelBuilder.Entity<LiveClassSchedule>()
                .Property(x => x.MaterialLink)
                .HasMaxLength(1000);

            modelBuilder.Entity<LiveClassSchedule>()
                .Property(x => x.MaterialFilePath)
                .HasMaxLength(500);

            modelBuilder.Entity<LiveClassSchedule>()
                .Property(x => x.IsCancelled)
                .HasDefaultValue(false);

            modelBuilder.Entity<LiveClassSchedule>()
                .Property(x => x.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<LiveClassSchedule>()
                .HasOne(x => x.Course)
                .WithMany()
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LiveClassSchedule>()
                .HasOne(x => x.Teacher)
                .WithMany()
                .HasForeignKey(x => x.TeacherId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<LiveClassSchedule>()
                .HasOne(x => x.ScheduledByAdmin)
                .WithMany()
                .HasForeignKey(x => x.ScheduledByAdminId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<LiveClassSchedule>()
                .HasIndex(x => new { x.CourseId, x.StartAt });

        }
    }
}
