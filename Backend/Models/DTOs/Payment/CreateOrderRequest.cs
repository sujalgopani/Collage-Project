namespace ExamNest.Models.DTOs.Payment
{
    public class CreateOrderRequest
    {
        public decimal amount { get; set; }
        public int CourseId { get; set; }
    }
}
