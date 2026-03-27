namespace ExamNest.Models
{
    public class ExamAttempt
    {
        public int ExamAttemptId { get; set; }
        public int ExamId { get; set; }
        public int StudentId { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public string Status { get; set; } = "InProgress";
        public int TotalScore { get; set; }
        public int MaxScore { get; set; }
        public int ViolationCount { get; set; }
        public bool IsFlagged { get; set; }
        public string ClientSignature { get; set; } = string.Empty;
        public string QuestionOrderCsv { get; set; } = string.Empty;
        public string OptionOrderJson { get; set; } = "{}";

        public Exam? Exam { get; set; }
        public User? Student { get; set; }
        public ICollection<ExamAttemptAnswer>? Answers { get; set; }
        public ICollection<ExamViolationEvent>? ViolationEvents { get; set; }
    }
}
