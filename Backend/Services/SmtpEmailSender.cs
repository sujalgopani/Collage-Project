using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;

namespace ExamNest.Services
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body, bool isBodyHtml = false)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                throw new ArgumentException("Recipient email is required.", nameof(toEmail));
            }

            var host = _configuration["Email:SmtpHost"];
            var port = int.TryParse(_configuration["Email:SmtpPort"], out var smtpPort) ? smtpPort : 587;
            var username = _configuration["Email:Username"];
            var password = _configuration["Email:Password"];
            var fromEmail = _configuration["Email:FromEmail"];
            var fromName = _configuration["Email:FromName"] ?? "ExamNest";
            var enableSsl = !bool.TryParse(_configuration["Email:EnableSsl"], out var parsedEnableSsl) || parsedEnableSsl;
            var maxAttempts = int.TryParse(_configuration["Email:SendRetryCount"], out var parsedRetryCount)
                ? Math.Clamp(parsedRetryCount, 1, 5)
                : 3;

            if (string.IsNullOrWhiteSpace(host) ||
                string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(fromEmail))
            {
                throw new InvalidOperationException("Email SMTP configuration is missing.");
            }

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    using var message = new MailMessage
                    {
                        From = new MailAddress(fromEmail, fromName),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = isBodyHtml
                    };

                    message.To.Add(toEmail);

                    using var smtp = new SmtpClient(host, port)
                    {
                        EnableSsl = enableSsl,
                        Credentials = new NetworkCredential(username, password),
                        DeliveryMethod = SmtpDeliveryMethod.Network,
                        UseDefaultCredentials = false
                    };

                    await smtp.SendMailAsync(message);
                    return;
                }
                catch (Exception ex) when (attempt < maxAttempts)
                {
                    _logger.LogWarning(
                        ex,
                        "Email send attempt {Attempt}/{MaxAttempts} failed for {Recipient}. Retrying.",
                        attempt,
                        maxAttempts,
                        toEmail);

                    await Task.Delay(TimeSpan.FromSeconds(attempt));
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Email send failed for {Recipient} after {MaxAttempts} attempt(s). Subject: {Subject}",
                        toEmail,
                        maxAttempts,
                        subject);
                    throw;
                }
            }
        }
    }
}
