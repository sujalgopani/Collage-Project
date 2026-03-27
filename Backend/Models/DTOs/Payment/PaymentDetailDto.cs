namespace ExamNest.Models.DTOs.Payment
{
    public class PaymentDetailDto
    {
		public int PaymentId { get; set; }
		public string? RazorpayPaymentId { get; set; }
		public decimal Amount { get; set; }
		public string? Status { get; set; }

		public string? CourseTitle { get; set; }
		public float CourseFees { get; set; }

		public string? StudentName { get; set; }
		public string? StudentEmail { get; set; }

		public string? OrderId { get; set; }
		public DateTime PaymentDate { get; set; }

		public string? SubscriptionStatus { get; set; }
	}
}
