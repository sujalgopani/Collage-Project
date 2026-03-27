using ExamNest.Data;
using Microsoft.EntityFrameworkCore;

namespace ExamNest.Services
{
    public class AppActivityEmailService
    {
        private readonly AppDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly IConfiguration _configuration;

        public AppActivityEmailService(
            AppDbContext context,
            IEmailSender emailSender,
            IConfiguration configuration)
        {
            _context = context;
            _emailSender = emailSender;
            _configuration = configuration;
        }

        public async Task NotifyAdminsAsync(
            string subject,
            string activityTitle,
            string summary,
            IReadOnlyDictionary<string, string>? detailsRows = null,
            string? actionPathOrUrl = null)
        {
            var admins = await _context.Users
                .AsNoTracking()
                .Where(u =>
                    u.IsActive &&
                    !string.IsNullOrWhiteSpace(u.Email) &&
                    (u.RoleId == 1 || (u.Role != null && u.Role.RoleName == "Admin")))
                .Select(u => u.Email!)
                .ToListAsync();

            var configuredAdmins = (_configuration["Email:AdminRecipients"] ?? string.Empty)
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            if (configuredAdmins.Count > 0)
            {
                admins.AddRange(configuredAdmins);
            }

            admins = admins
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (admins.Count == 0)
                return;

            var actionUrl = ResolveActionUrl(actionPathOrUrl);
            var body = EmailTemplateBuilder.BuildAdminActivityEmail(
                activityTitle,
                summary,
                detailsRows,
                actionUrl);

            foreach (var email in admins)
            {
                try
                {
                    await _emailSender.SendEmailAsync(email, subject, body, isBodyHtml: true);
                }
                catch
                {
                    // Do not block core flow if one admin email fails.
                }
            }
        }

        private string? ResolveActionUrl(string? actionPathOrUrl)
        {
            if (string.IsNullOrWhiteSpace(actionPathOrUrl))
                return null;

            if (Uri.TryCreate(actionPathOrUrl, UriKind.Absolute, out _))
                return actionPathOrUrl;

            var baseUrl = GetFrontendBaseUrl();
            return $"{baseUrl}/{actionPathOrUrl.TrimStart('/')}";
        }

        private string GetFrontendBaseUrl()
        {
            var configured = _configuration["Frontend:BaseUrl"];
            return string.IsNullOrWhiteSpace(configured)
                ? "http://localhost:4200"
                : configured.TrimEnd('/');
        }
    }
}
