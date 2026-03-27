using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ExamNest.Data;
using ExamNest.Models;
using ExamNest.Models.DTOs.Payment;
using ExamNest.Models.Payment;
using ExamNest.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Razorpay.Api;

namespace ExamNest.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
	[Authorize(Roles = "Admin,Teacher,Student")]
	public class PaymentController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly AppDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly AppActivityEmailService _activityEmailService;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(
            IConfiguration config,
            AppDbContext context,
            IEmailSender emailSender,
            AppActivityEmailService activityEmailService,
            ILogger<PaymentController> logger)
        {
            _config = config;
            _context = context;
            _emailSender = emailSender;
            _activityEmailService = activityEmailService;
            _logger = logger;
        }

        // ===============================
        // 1️⃣ CREATE ORDER
        // ===============================

        [HttpPost("create-order")]
        public async Task<IActionResult> CreateOrder(CreateOrderRequest request)
        {
            if (request.CourseId <= 0)
                return BadRequest("Valid course id is required.");

            var course = await _context.Courses
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CourseId == request.CourseId && c.IsPublished);

            if (course == null)
                return NotFound("Course not found or not available for subscription.");

            var key = _config["Razorpay:Key"];
            var secret = _config["Razorpay:SecretKey"];

            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(secret))
                return StatusCode(500, "Payment gateway is not configured.");

            RazorpayClient client = new RazorpayClient(key, secret);

            var studentIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                ?? User.FindFirst("sub")?.Value;
            if (!int.TryParse(studentIdClaim, out var studentId))
                return Unauthorized("Invalid student token.");
            var hasActiveSubscription = await _context.Subscriptions
                .AsNoTracking()
                .AnyAsync(s => s.StudentId == studentId && s.CourseId == request.CourseId && s.Status == "Active");

            if (hasActiveSubscription)
                return Conflict("You are already subscribed to this course.");

            var normalizedAmount = decimal.Round(Convert.ToDecimal(course.Fees), 2, MidpointRounding.AwayFromZero);
            if (normalizedAmount <= 0)
                return BadRequest("Course amount is invalid.");

            var amountInPaise = (int)decimal.Round(normalizedAmount * 100m, MidpointRounding.AwayFromZero);
            if (amountInPaise <= 0)
                return BadRequest("Course amount is invalid.");

            Dictionary<string, object> options = new Dictionary<string, object>();

            options.Add("amount", amountInPaise); // Razorpay paisa
            options.Add("currency", "INR");
            options.Add("payment_capture", 1);

            Razorpay.Api.Order razorpayOrder = client.Order.Create(options);

            string razorpayOrderId = razorpayOrder["id"].ToString();

            // Save Order in DB
            var order = new Models.Payment.Order
            {
                OrderId = razorpayOrderId,
                StudentId = studentId,
                CourseId = request.CourseId,
                Amount = normalizedAmount,
                Status = "Pending",
                CreatedAt = DateTime.Now
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            try
            {
                var student = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.UserId == studentId);

                await _activityEmailService.NotifyAdminsAsync(
                    subject: $"Activity: Payment Order Created - {course.Title}",
                    activityTitle: "Student Initiated Payment",
                    summary: "A student created a payment order and is likely to complete checkout.",
                    detailsRows: new Dictionary<string, string>
                    {
                        ["Course"] = course.Title,
                        ["Order ID"] = razorpayOrderId,
                        ["Student"] = student != null ? GetDisplayName(student) : $"Student #{studentId}",
                        ["Amount"] = $"INR {normalizedAmount:0.00}"
                    },
                    actionPathOrUrl: "/admin-dashboard/payment-manage");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to notify admins for order creation {OrderId}.", razorpayOrderId);
            }

            return Ok(new
            {
                orderId = razorpayOrderId,
                amount = normalizedAmount,
                key = key
            });
        }

        // ===============================
        // 2️⃣ VERIFY PAYMENT
        // ===============================

        [HttpPost("verify-payment")]
        public async Task<IActionResult> VerifyPayment(VerifyPaymentRequest request)
        {
            if (request == null ||
                string.IsNullOrWhiteSpace(request.razorpay_order_id) ||
                string.IsNullOrWhiteSpace(request.razorpay_payment_id) ||
                string.IsNullOrWhiteSpace(request.razorpay_signature))
            {
                return BadRequest("Invalid payment verification payload.");
            }

            var secret = _config["Razorpay:SecretKey"];
            if (string.IsNullOrWhiteSpace(secret))
                return StatusCode(500, "Payment gateway is not configured.");

            string generatedSignature = GenerateSignature(
                request.razorpay_order_id + "|" + request.razorpay_payment_id,
                secret
            );

            if (generatedSignature != request.razorpay_signature)
            {
                return BadRequest("Payment Verification Failed");
            }

            var existingPayment = await _context.Payments
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.RazorpayPaymentId == request.razorpay_payment_id);

            if (existingPayment != null)
            {
                return Ok("Payment already verified and subscription active");
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

            var existingSubscription = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.StudentId == order.StudentId && s.CourseId == order.CourseId);

            if (existingSubscription == null)
            {
                _context.Subscriptions.Add(subscription);
            }
            else
            {
                existingSubscription.Status = "Active";
                existingSubscription.CreatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            await NotifyPaymentSuccessAsync(order, payment);

            return Ok("Payment Success and Subscription Created");
        }

        private async Task NotifyPaymentSuccessAsync(ExamNest.Models.Payment.Order order, ExamNest.Models.Payment.Payment payment)
        {
            var contextOrder = await _context.Orders
                .AsNoTracking()
                .Include(o => o.Student)
                .Include(o => o.Course)
                    .ThenInclude(c => c!.Teacher)
                .FirstOrDefaultAsync(o => o.Id == order.Id);

            if (contextOrder?.Course == null)
                return;

            var student = contextOrder.Student;
            var teacher = contextOrder.Course.Teacher;

            var frontendBase = GetFrontendBaseUrl();
            var studentCourseUrl = $"{frontendBase}/student-dashboard/learn-courses";
            var teacherSubscribersUrl = $"{frontendBase}/teacher-dashboard/my-subscriber";

            if (student != null && !string.IsNullOrWhiteSpace(student.Email))
            {
                var studentBody = EmailTemplateBuilder.BuildEnrollmentConfirmedEmail(
                    GetDisplayName(student),
                    contextOrder.Course.Title,
                    payment.Amount,
                    payment.RazorpayPaymentId,
                    payment.CreatedAt,
                    studentCourseUrl,
                    orderId: contextOrder.OrderId,
                    teacherName: teacher != null ? GetDisplayName(teacher) : null);

                try
                {
                    await _emailSender.SendEmailAsync(
                        student.Email,
                        $"Enrollment Confirmed: {contextOrder.Course.Title}",
                        studentBody,
                        isBodyHtml: true);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to send payment confirmation email to student {StudentId} for order {OrderId}.",
                        contextOrder.StudentId,
                        contextOrder.OrderId);
                }
            }

            if (teacher != null && !string.IsNullOrWhiteSpace(teacher.Email))
            {
                var teacherBody = EmailTemplateBuilder.BuildNewEnrollmentForTeacherEmail(
                    GetDisplayName(teacher),
                    student != null ? GetDisplayName(student) : "Student",
                    contextOrder.Course.Title,
                    payment.Amount,
                    payment.CreatedAt,
                    teacherSubscribersUrl,
                    paymentId: payment.RazorpayPaymentId,
                    orderId: contextOrder.OrderId);

                try
                {
                    await _emailSender.SendEmailAsync(
                        teacher.Email,
                        $"New Enrollment: {contextOrder.Course.Title}",
                        teacherBody,
                        isBodyHtml: true);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to send enrollment email to teacher {TeacherId} for order {OrderId}.",
                        contextOrder.Course.TeacherId,
                        contextOrder.OrderId);
                }
            }

            try
            {
                await _activityEmailService.NotifyAdminsAsync(
                    subject: $"Activity: Payment Success - {contextOrder.Course.Title}",
                    activityTitle: "Payment Verified and Subscription Created",
                    summary: "A student completed payment and subscription was activated.",
                    detailsRows: new Dictionary<string, string>
                    {
                        ["Course"] = contextOrder.Course.Title,
                        ["Student"] = student != null ? GetDisplayName(student) : $"Student #{contextOrder.StudentId}",
                        ["Teacher"] = teacher != null ? GetDisplayName(teacher) : "Teacher",
                        ["Amount"] = $"INR {payment.Amount:0.00}",
                        ["Payment ID"] = payment.RazorpayPaymentId,
                        ["Order ID"] = contextOrder.OrderId
                    },
                    actionPathOrUrl: "/admin-dashboard/payment-manage");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send admin payment activity email for order {OrderId}.", contextOrder.OrderId);
            }
        }

        private string GetFrontendBaseUrl()
        {
            var configured = _config["Frontend:BaseUrl"];
            return string.IsNullOrWhiteSpace(configured)
                ? "http://localhost:4200"
                : configured.TrimEnd('/');
        }

        private static string GetDisplayName(User user)
        {
            var display = string.Join(
                " ",
                new[] { user.FirstName, user.MiddleName, user.LastName }
                    .Where(x => !string.IsNullOrWhiteSpace(x)));

            return string.IsNullOrWhiteSpace(display) ? user.Username : display;
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
