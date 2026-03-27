namespace ExamNest.Models
{
    public class ExamAttemptAnswer
    {
        public int ExamAttemptAnswerId { get; set; }
        public int ExamAttemptId { get; set; }
        public int ExamQuestionId { get; set; }
        public string SelectedOption { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
        public int MarksAwarded { get; set; }
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        public ExamAttempt? ExamAttempt { get; set; }
        public ExamQuestion? ExamQuestion { get; set; }
    }
}
