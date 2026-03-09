namespace ExamNest.Models.DTOs.Student
{
    public class CourseCardStudentDTO
    {
        public int CourseId { get; set; }
        public string Title { get; set; } = "";
        public string ThumbnailUrl { get; set; } = "";
        public float Fees { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
