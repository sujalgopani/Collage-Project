using System.Text.Json.Serialization;

namespace ExamNest.Models
{
    public class Exam
    {
        public int ExamId { get; set; }
        public int CourseId { get; set; }
        public int TeacherId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
        public int DurationMinutes { get; set; }
        public int RandomQuestionCount { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsResultPublished { get; set; }
        public DateTime? ResultPublishedAt { get; set; }

        public Course? Course { get; set; }
        public User? Teacher { get; set; }
		public ICollection<ExamAttempt>? ExamAttempts { get; set; }
		public ICollection<ExamQuestion>? Questions { get; set; }
    }
}
