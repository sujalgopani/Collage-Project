namespace ExamNest.Models.DTOs.Exam
{
    public class ExamViolationRequestDto
    {
        public int ExamAttemptId { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string? Details { get; set; }
    }
}
