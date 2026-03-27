namespace ExamNest.Models.DTOs.Exam
{
    public class ExamStudentDto
    {
		public int UserId { get; set; }
		public string? Name { get; set; }
		public string? Email { get; set; }
        public int MaxScore { get; set; }
        public int Score { get; set; }
		public string? Status { get; set; }
	}
}
