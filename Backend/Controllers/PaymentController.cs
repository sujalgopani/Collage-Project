using ExamNest.Data;
using ExamNest.Models.DTOs.Payment;
using ExamNest.Models.Payment;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Razorpay.Api;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ExamNest.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly AppDbContext _context;

        public PaymentController(IConfiguration config, AppDbContext context)
        {
            _config = config;
            _context = context;
        }

        // ===============================
        // 1️⃣ CREATE ORDER
        // ===============================

        [HttpPost("create-order")]
        public async Task<IActionResult> CreateOrder(CreateOrderRequest request)
        {
            var key = _config["Razorpay:Key"];
            var secret = _config["Razorpay:SecretKey"];

            RazorpayClient client = new RazorpayClient(key, secret);

            Dictionary<string, object> options = new Dictionary<string, object>();

            options.Add("amount", request.amount * 100); // Razorpay paisa
            options.Add("currency", "INR");
            options.Add("payment_capture", 1);

            Razorpay.Api.Order razorpayOrder = client.Order.Create(options);

            string razorpayOrderId = razorpayOrder["id"].ToString();
            var studentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

            // Save Order in DB
            var order = new Models.Payment.Order
            {
                OrderId = razorpayOrderId,
                StudentId = studentId,
                CourseId = request.CourseId,
                Amount = request.amount,
                Status = "Pending",
                CreatedAt = DateTime.Now
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                orderId = razorpayOrderId,
                amount = request.amount,
                key = key
            });
        }

        // ===============================
        // 2️⃣ VERIFY PAYMENT
        // ===============================

        [HttpPost("verify-payment")]
        public async Task<IActionResult> VerifyPayment(VerifyPaymentRequest request)
        {
            var secret = _config["Razorpay:SecretKey"];

            string generatedSignature = GenerateSignature(
                request.razorpay_order_id + "|" + request.razorpay_payment_id,
                secret!
            );

            if (generatedSignature != request.razorpay_signature)
            {
                return BadRequest("Payment Verification Failed");
            }

            // Find Order
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderId == request.razorpay_order_id);

            if (order == null)
                return NotFound("Order Not Found");

            order.Status = "Paid";

            // ===============================
            // Save Payment
            // ===============================

            var payment = new Models.Payment.Payment
            {
                RazorpayPaymentId = request.razorpay_payment_id,
                RazorpayOrderId = request.razorpay_order_id,
                Signature = request.razorpay_signature,
                Amount = order.Amount,
                Status = "Success",
                OrderId = order.Id,   // ✅ FIX HERE
                CreatedAt = DateTime.Now
            };



            // ===============================
            // Create Subscription
            // ===============================

            var subscription = new Models.Payment.Subscription
            {
                StudentId = order.StudentId,
                CourseId = order.CourseId,
                Status = "Active",
                CreatedAt = DateTime.Now
            };

            _context.Payments.Add(payment);
            _context.Subscriptions.Add(subscription);

            await _context.SaveChangesAsync();

            return Ok("Payment Success and Subscription Created");
        }

        // ===============================
        // Signature Generator
        // ===============================

        private string GenerateSignature(string text, string secret)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(secret);
            byte[] textBytes = Encoding.UTF8.GetBytes(text);

            using (var hmac = new HMACSHA256(keyBytes))
            {
                byte[] hash = hmac.ComputeHash(textBytes);
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }
    }
}