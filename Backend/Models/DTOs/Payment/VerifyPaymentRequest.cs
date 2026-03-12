namespace ExamNest.Models.DTOs.Payment
{
    public class VerifyPaymentRequest
    {
        public string razorpay_order_id { get; set; } = "";
        public string razorpay_payment_id { get; set; } = "";
        public string razorpay_signature { get; set; } = "";
    }
}
