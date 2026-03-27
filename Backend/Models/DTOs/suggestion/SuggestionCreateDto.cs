namespace ExamNest.Models.DTOs.suggestion
{
    public class SuggestionCreateDto
    {
        public int TeacherId { get; set; }
        public string? Title { get; set; }
        public string? Message { get; set; }
    }
}
