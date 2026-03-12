namespace ExamNest.Models
{
    public class ExamViolationEvent
    {
        public int ExamViolationEventId { get; set; }
        public int ExamAttemptId { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string? Details { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ExamAttempt? ExamAttempt { get; set; }
    }
}
