namespace ExamNest.Models.DTOs
{
    public class CourseCreateDTO
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public float Fees { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public IFormFile ? ThumbailUrl { get; set; }
        public List<IFormFile>? Files { get; set; }
    }
}
