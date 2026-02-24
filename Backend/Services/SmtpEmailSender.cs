using System.Net;
using System.Net.Mail;

namespace ExamNest.Services
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;

        public SmtpEmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var host = _configuration["Email:SmtpHost"];
            var port = int.TryParse(_configuration["Email:SmtpPort"], out var smtpPort) ? smtpPort : 587;
            var username = _configuration["Email:Username"];
            var password = _configuration["Email:Password"];
            var fromEmail = _configuration["Email:FromEmail"];
            var fromName = _configuration["Email:FromName"] ?? "ExamNest";

            if (string.IsNullOrWhiteSpace(host) ||
                string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(fromEmail))
            {
                throw new InvalidOperationException("Email SMTP configuration is missing.");
            }

            using var message = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };

            message.To.Add(toEmail);

            using var smtp = new SmtpClient(host, port)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(username, password)
            };

            await smtp.SendMailAsync(message);
        }
    }
}
