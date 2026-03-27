namespace ExamNest.Models.DTOs.Exam
{
    public class SubmitExamRequestDto
    {
        public int ExamAttemptId { get; set; }
        public List<SubmitExamAnswerDto> Answers { get; set; } = new();
    }

    public class SubmitExamAnswerDto
    {
        public int ExamQuestionId { get; set; }
        public string SelectedOption { get; set; } = string.Empty;
    }
}
