using System.Net;

namespace ExamNest.Services
{
    public static class EmailTemplateBuilder
    {
        public static string BuildOtpEmail(string otp, int otpExpiryMinutes)
        {
            return BuildLayout(
                title: "Verify your email",
                subtitle: "Use the OTP below to complete your ExamNest verification.",
                highlightValue: otp,
                details: $"This OTP expires in {otpExpiryMinutes} minutes.");
        }

        public static string BuildVerifiedCredentialsEmail(string username, string passwordDisplay)
        {
            return BuildLayout(
                title: "Your account is verified",
                subtitle: "You can now login to ExamNest using the credentials below.",
                details: $"<strong>Username:</strong> {Encode(username)}<br/><strong>Password:</strong> {Encode(passwordDisplay)}");
        }

        public static string BuildNewExamNotificationEmail(string studentName, string courseTitle, string examTitle, DateTime startAt, DateTime endAt, int durationMinutes)
        {
            return BuildLayout(
                title: $"Hi {Encode(studentName)}, a new exam is available",
                subtitle: $"A teacher has created a new exam in your subscribed course <strong>{Encode(courseTitle)}</strong>.",
                highlightValue: Encode(examTitle),
                details: $"<strong>Starts:</strong> {startAt:dd MMM yyyy, hh:mm tt}<br/><strong>Ends:</strong> {endAt:dd MMM yyyy, hh:mm tt}<br/><strong>Duration:</strong> {durationMinutes} minutes");
        }

        private static string BuildLayout(string title, string subtitle, string? highlightValue = null, string? details = null)
        {
            var encodedTitle = title;
            var encodedSubtitle = subtitle;

            return $"""
<!doctype html>
<html>
<head>
  <meta charset=\"UTF-8\" />
  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\" />
</head>
<body style=\"margin:0;background:#f6f8fb;font-family:Arial,Helvetica,sans-serif;color:#1f2937;\">
  <div style=\"max-width:620px;margin:24px auto;padding:0 12px;\">
    <div style=\"background:#ffffff;border:1px solid #e5e7eb;border-radius:10px;overflow:hidden;\">
      <div style=\"background:#1d4ed8;color:#ffffff;padding:18px 20px;font-size:20px;font-weight:700;\">ExamNest</div>
      <div style=\"padding:24px 20px;\">
        <h2 style=\"margin:0 0 8px;font-size:22px;\">{encodedTitle}</h2>
        <p style=\"margin:0 0 16px;line-height:1.5;\">{encodedSubtitle}</p>
        {(string.IsNullOrWhiteSpace(highlightValue) ? string.Empty : $"<div style=\"margin:0 0 16px;padding:14px;background:#eff6ff;border:1px solid #bfdbfe;border-radius:8px;font-size:24px;font-weight:700;letter-spacing:2px;text-align:center;\">{highlightValue}</div>")}
        {(string.IsNullOrWhiteSpace(details) ? string.Empty : $"<p style=\"margin:0;line-height:1.6;\">{details}</p>")}
      </div>
    </div>
  </div>
</body>
</html>
""";
        }

        private static string Encode(string value) => WebUtility.HtmlEncode(value);
    }
}
