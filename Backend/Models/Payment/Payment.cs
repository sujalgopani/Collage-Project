using System.ComponentModel.DataAnnotations;

namespace ExamNest.Models.Payment
{
    public class Payment
    {
        public int Id { get; set; }

        public string RazorpayPaymentId { get; set; } = "";
        public string RazorpayOrderId { get; set; } = "";
        public string Signature { get; set; } = "";

        public decimal Amount { get; set; }
        public string Status { get; set; } = ""; // Success / Failed

        public int OrderId { get; set; }
        public Order ?Order { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
