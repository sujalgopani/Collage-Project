using System.ComponentModel.DataAnnotations;

namespace ExamNest.Models.Payment
{
    public class Order
    {

        public int Id { get; set; }
        public string OrderId { get; set; } = ""; // Razorpay OrderId

        public int StudentId { get; set; }
        public int CourseId { get; set; }

        public decimal Amount { get; set; }
        public string Status { get; set; } = ""; // Pending / Paid

        public DateTime CreatedAt { get; set; }

        public User ?Student { get; set; }
        public Course ?Course { get; set; }
    }
}
