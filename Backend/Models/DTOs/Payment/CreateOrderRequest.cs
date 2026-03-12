namespace ExamNest.Models.DTOs.Payment
{
    public class CreateOrderRequest
    {
        public int amount { get; set; }
        public int CourseId { get; set; }
    }
}
