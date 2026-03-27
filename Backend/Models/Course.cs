using Microsoft.VisualBasic;

namespace ExamNest.Models
{
    public class Course
    {
        public int CourseId { get; set; }

        public string Title { get; set; } = "";
        public string Description { get; set; } = "";

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsPublished { get; set; } = false;

        public float Fees { get; set; }
        public string ThumbailUrl { get; set; } = "";
        public int TeacherId { get; set; }   // FK → User table
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public User? Teacher { get; set; }
        public ICollection<CourseMedia>? CourseMedias { get; set; }

    }
}
