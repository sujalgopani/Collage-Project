namespace ExamNest.Models.DTOs.Exam
{
    public class ExamUploadFromExcelDto
    {
        public int CourseId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
        public int DurationMinutes { get; set; } = 60;
        public int RandomQuestionCount { get; set; } = 20;
        public IFormFile? ExcelFile { get; set; }
    }
}
