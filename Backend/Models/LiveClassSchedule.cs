namespace ExamNest.Models
{
    public class LiveClassSchedule
    {
        public int LiveClassScheduleId { get; set; }

        public int CourseId { get; set; }
        public int TeacherId { get; set; }
        public int ScheduledByAdminId { get; set; }

        public string Title { get; set; } = "";
        public string? Agenda { get; set; }
        public string MeetingLink { get; set; } = "";

        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }

        public string? MaterialTitle { get; set; }
        public string? MaterialDescription { get; set; }
        public string? MaterialLink { get; set; }
        public string? MaterialFilePath { get; set; }
        public bool IsCancelled { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public Course? Course { get; set; }
        public User? Teacher { get; set; }
        public User? ScheduledByAdmin { get; set; }
    }
}
