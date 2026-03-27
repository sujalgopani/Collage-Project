using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExamNest.Models
{
    public class Suggestion
    {
        public int Id { get; set; }

        public int StudentId { get; set; }

        [ForeignKey("StudentId")]   // ✅ IMPORTANT
        public User ?Student { get; set; }

        public int TeacherId { get; set; }

        [ForeignKey("TeacherId")]   // optional but recommended
        public User ?Teacher { get; set; }

        public string? Title { get; set; }
        public string ?Message { get; set; }
        public string? Reply { get; set; }
        public string ?Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
