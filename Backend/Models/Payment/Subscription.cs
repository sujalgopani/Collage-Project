using ExamNest.Services;
using System.ComponentModel.DataAnnotations;

namespace ExamNest.Models.Payment
{
    public class Subscription
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public int CourseId { get; set; }

        public string Status { get; set; } = ""; // Active

        public DateTime CreatedAt { get; set; }

        public User? Student { get; set; }
        public Course? Course { get; set; }
    }
}
