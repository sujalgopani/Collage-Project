using System.Net;
using System.Text;

namespace ExamNest.Services
{
    public static class EmailTemplateBuilder
    {
        public static string BuildOtpEmail(string otp, int otpExpiryMinutes)
        {
            return BuildLayout(
                title: "Verify your email",
                subtitle: "Use this one-time password to complete your ExamNest verification.",
                highlightValue: otp,
                badgeText: "Security Check",
                detailsRows: new Dictionary<string, string>
                {
                    ["OTP Expiry"] = $"{otpExpiryMinutes} minutes",
                    ["If you did not request this"] = "You can safely ignore this email."
                });
        }

        public static string BuildVerifiedCredentialsEmail(string username, string passwordDisplay)
        {
            return BuildLayout(
                title: "Your account is ready",
                subtitle: "You can now sign in to ExamNest with your verified credentials.",
                badgeText: "Account Verified",
                detailsRows: new Dictionary<string, string>
                {
                    ["Username"] = username,
                    ["Password"] = passwordDisplay
                },
                helperNote: "For better security, change your password after first login.");
        }

        public static string BuildNewExamNotificationEmail(
            string studentName,
            string courseTitle,
            string examTitle,
            DateTime startAt,
            DateTime endAt,
            int durationMinutes,
            string? actionUrl = null)
        {
            return BuildLayout(
                title: $"Hi {studentName}, a new exam is available",
                subtitle: $"A new exam was created for your course \"{courseTitle}\".",
                highlightValue: examTitle,
                badgeText: "New Exam",
                detailsRows: new Dictionary<string, string>
                {
                    ["Starts"] = startAt.ToString("dd MMM yyyy, hh:mm tt"),
                    ["Ends"] = endAt.ToString("dd MMM yyyy, hh:mm tt"),
                    ["Duration"] = $"{durationMinutes} minutes"
                },
                actionLabel: "Open Student Exams",
                actionUrl: actionUrl);
        }

        public static string BuildCoursePublishedEmail(
            string teacherName,
            string courseTitle,
            DateTime startDate,
            DateTime endDate,
            decimal fees,
            string? actionUrl = null)
        {
            return BuildLayout(
                title: $"Hi {teacherName}, your course is now published",
                subtitle: "An administrator has approved and published your course on ExamNest.",
                highlightValue: courseTitle,
                badgeText: "Course Published",
                detailsRows: new Dictionary<string, string>
                {
                    ["Start Date"] = startDate.ToString("dd MMM yyyy"),
                    ["End Date"] = endDate.ToString("dd MMM yyyy"),
                    ["Fees"] = $"INR {fees:0.00}"
                },
                actionLabel: "Manage Course",
                actionUrl: actionUrl);
        }

        public static string BuildSuggestionSubmittedEmail(
            string teacherName,
            string studentName,
            string suggestionTitle,
            string suggestionMessage,
            string? actionUrl = null)
        {
            return BuildLayout(
                title: $"Hi {teacherName}, you received a new suggestion",
                subtitle: $"Student {studentName} sent feedback that needs your review.",
                highlightValue: suggestionTitle,
                badgeText: "New Suggestion",
                detailsRows: new Dictionary<string, string>
                {
                    ["From Student"] = studentName,
                    ["Message"] = suggestionMessage
                },
                actionLabel: "Review Suggestions",
                actionUrl: actionUrl);
        }

        public static string BuildSuggestionReplyEmail(
            string studentName,
            string teacherName,
            string suggestionTitle,
            string reply,
            string? actionUrl = null)
        {
            return BuildLayout(
                title: $"Hi {studentName}, your suggestion has a reply",
                subtitle: $"{teacherName} responded to your feedback.",
                highlightValue: suggestionTitle,
                badgeText: "Reply Received",
                detailsRows: new Dictionary<string, string>
                {
                    ["Teacher"] = teacherName,
                    ["Reply"] = reply
                },
                actionLabel: "View My Suggestions",
                actionUrl: actionUrl);
        }

        public static string BuildEnrollmentConfirmedEmail(
            string studentName,
            string courseTitle,
            decimal amount,
            string paymentId,
            DateTime enrolledAt,
            string? actionUrl = null,
            string? orderId = null,
            string? teacherName = null)
        {
            var details = new Dictionary<string, string>
            {
                ["Amount"] = $"INR {amount:0.00}",
                ["Payment Reference"] = paymentId,
                ["Enrolled At"] = enrolledAt.ToString("dd MMM yyyy, hh:mm tt")
            };

            if (!string.IsNullOrWhiteSpace(orderId))
            {
                details["Order ID"] = orderId;
            }

            if (!string.IsNullOrWhiteSpace(teacherName))
            {
                details["Teacher"] = teacherName;
            }

            return BuildLayout(
                title: $"Hi {studentName}, enrollment confirmed",
                subtitle: "Your payment was successful and your course subscription is active.",
                highlightValue: courseTitle,
                badgeText: "Payment Success",
                detailsRows: details,
                actionLabel: "Start Learning",
                actionUrl: actionUrl);
        }

        public static string BuildNewEnrollmentForTeacherEmail(
            string teacherName,
            string studentName,
            string courseTitle,
            decimal amount,
            DateTime enrolledAt,
            string? actionUrl = null,
            string? paymentId = null,
            string? orderId = null)
        {
            var details = new Dictionary<string, string>
            {
                ["Student"] = studentName,
                ["Amount"] = $"INR {amount:0.00}",
                ["Enrolled At"] = enrolledAt.ToString("dd MMM yyyy, hh:mm tt")
            };

            if (!string.IsNullOrWhiteSpace(paymentId))
            {
                details["Payment Reference"] = paymentId;
            }

            if (!string.IsNullOrWhiteSpace(orderId))
            {
                details["Order ID"] = orderId;
            }

            return BuildLayout(
                title: $"Hi {teacherName}, new student enrollment",
                subtitle: "A student has completed payment and joined your course.",
                highlightValue: courseTitle,
                badgeText: "New Enrollment",
                detailsRows: details,
                actionLabel: "View Subscribers",
                actionUrl: actionUrl);
        }

        public static string BuildExamResultPublishedEmail(
            string studentName,
            string examTitle,
            string courseTitle,
            int totalScore,
            int maxScore,
            string? actionUrl = null)
        {
            return BuildLayout(
                title: $"Hi {studentName}, your result is published",
                subtitle: $"Results are now available for \"{examTitle}\" in \"{courseTitle}\".",
                highlightValue: $"{totalScore}/{maxScore}",
                badgeText: "Result Published",
                detailsRows: new Dictionary<string, string>
                {
                    ["Exam"] = examTitle,
                    ["Course"] = courseTitle,
                    ["Score"] = $"{totalScore} out of {maxScore}"
                },
                actionLabel: "Open Results",
                actionUrl: actionUrl);
        }

        public static string BuildExamStartedEmail(
            string studentName,
            string examTitle,
            string courseTitle,
            DateTime startedAt,
            DateTime expiresAt,
            string? actionUrl = null)
        {
            return BuildLayout(
                title: $"Hi {studentName}, your exam session has started",
                subtitle: $"You started \"{examTitle}\" for \"{courseTitle}\".",
                badgeText: "Exam Started",
                detailsRows: new Dictionary<string, string>
                {
                    ["Exam"] = examTitle,
                    ["Course"] = courseTitle,
                    ["Started At"] = startedAt.ToString("dd MMM yyyy, hh:mm tt"),
                    ["Must Submit By"] = expiresAt.ToString("dd MMM yyyy, hh:mm tt")
                },
                actionLabel: "Open Exam Dashboard",
                actionUrl: actionUrl);
        }

        public static string BuildNewMediaUploadedEmail(
            string studentName,
            string courseTitle,
            int uploadedCount,
            string teacherName,
            string? actionUrl = null)
        {
            return BuildLayout(
                title: $"Hi {studentName}, new content is available",
                subtitle: $"{teacherName} uploaded new learning media for your subscribed course.",
                highlightValue: courseTitle,
                badgeText: "New Media Uploaded",
                detailsRows: new Dictionary<string, string>
                {
                    ["Course"] = courseTitle,
                    ["Uploaded Items"] = uploadedCount.ToString(),
                    ["Uploaded By"] = teacherName
                },
                actionLabel: "Watch New Lessons",
                actionUrl: actionUrl);
        }

        public static string BuildLiveClassScheduledEmail(
            string recipientName,
            string courseTitle,
            string classTitle,
            DateTime startAt,
            DateTime endAt,
            string meetingLink,
            string? agenda = null,
            string? actionUrl = null)
        {
            return BuildLayout(
                title: $"Hi {recipientName}, a live class is scheduled",
                subtitle: $"A new live class was scheduled for \"{courseTitle}\".",
                highlightValue: classTitle,
                badgeText: "Live Class Scheduled",
                detailsRows: new Dictionary<string, string>
                {
                    ["Course"] = courseTitle,
                    ["Starts"] = startAt.ToString("dd MMM yyyy, hh:mm tt"),
                    ["Ends"] = endAt.ToString("dd MMM yyyy, hh:mm tt"),
                    ["Meeting Link"] = meetingLink,
                    ["Agenda"] = agenda ?? string.Empty
                },
                actionLabel: "Open Live Classes",
                actionUrl: actionUrl);
        }

        public static string BuildLiveClassCancelledEmail(
            string recipientName,
            string courseTitle,
            string classTitle,
            DateTime startAt,
            DateTime endAt,
            string? actionUrl = null)
        {
            return BuildLayout(
                title: $"Hi {recipientName}, a live class was cancelled",
                subtitle: $"The live class for \"{courseTitle}\" was cancelled by the administrator.",
                highlightValue: classTitle,
                badgeText: "Live Class Cancelled",
                detailsRows: new Dictionary<string, string>
                {
                    ["Course"] = courseTitle,
                    ["Scheduled Start"] = startAt.ToString("dd MMM yyyy, hh:mm tt"),
                    ["Scheduled End"] = endAt.ToString("dd MMM yyyy, hh:mm tt")
                },
                actionLabel: "View Live Classes",
                actionUrl: actionUrl);
        }

        public static string BuildLiveClassMaterialUpdatedEmail(
            string recipientName,
            string courseTitle,
            string classTitle,
            string materialTitle,
            string? actionUrl = null)
        {
            return BuildLayout(
                title: $"Hi {recipientName}, live class material is updated",
                subtitle: $"New material was shared for your live class in \"{courseTitle}\".",
                highlightValue: classTitle,
                badgeText: "Material Updated",
                detailsRows: new Dictionary<string, string>
                {
                    ["Course"] = courseTitle,
                    ["Live Class"] = classTitle,
                    ["Material"] = materialTitle
                },
                actionLabel: "Open Live Classes",
                actionUrl: actionUrl);
        }

        public static string BuildAdminActivityEmail(
            string activityTitle,
            string summary,
            IReadOnlyDictionary<string, string>? detailsRows = null,
            string? actionUrl = null)
        {
            return BuildLayout(
                title: activityTitle,
                subtitle: summary,
                badgeText: "App Activity",
                detailsRows: detailsRows,
                actionLabel: "Open Admin Panel",
                actionUrl: actionUrl);
        }

        private static string BuildLayout(
            string title,
            string subtitle,
            IReadOnlyDictionary<string, string>? detailsRows = null,
            string? highlightValue = null,
            string? badgeText = null,
            string? actionLabel = null,
            string? actionUrl = null,
            string? helperNote = null)
        {
            var safeTitle = Encode(title);
            var safeSubtitle = Encode(subtitle);
            var safeHighlight = string.IsNullOrWhiteSpace(highlightValue) ? string.Empty : Encode(highlightValue);
            var safeBadge = string.IsNullOrWhiteSpace(badgeText) ? string.Empty : Encode(badgeText);
            var safeActionLabel = string.IsNullOrWhiteSpace(actionLabel) ? string.Empty : Encode(actionLabel);
            var safeActionUrl = string.IsNullOrWhiteSpace(actionUrl) ? string.Empty : Encode(actionUrl);
            var safeHelperNote = string.IsNullOrWhiteSpace(helperNote) ? string.Empty : Encode(helperNote);

            var detailsHtml = BuildDetailsTable(detailsRows);

            var badgeHtml = string.IsNullOrWhiteSpace(safeBadge)
                ? string.Empty
                : $"<div style=\"display:inline-block;margin:0 0 14px;padding:6px 10px;background:#e0e7ff;color:#1e3a8a;border-radius:999px;font-size:12px;font-weight:700;letter-spacing:.04em;\">{safeBadge}</div>";

            var highlightHtml = string.IsNullOrWhiteSpace(safeHighlight)
                ? string.Empty
                : $"<div style=\"margin:0 0 18px;padding:14px 16px;background:#eff6ff;border:1px solid #bfdbfe;border-radius:10px;font-size:22px;font-weight:700;text-align:center;color:#1e3a8a;\">{safeHighlight}</div>";

            var actionHtml = !string.IsNullOrWhiteSpace(safeActionLabel) && !string.IsNullOrWhiteSpace(safeActionUrl)
                ? $"""
<div style="margin:18px 0 0;">
  <a href="{safeActionUrl}" style="display:inline-block;background:#2563eb;color:#ffffff;text-decoration:none;font-weight:700;padding:12px 18px;border-radius:8px;">
    {safeActionLabel}
  </a>
</div>
"""
                : string.Empty;

            var helperHtml = string.IsNullOrWhiteSpace(safeHelperNote)
                ? string.Empty
                : $"<p style=\"margin:18px 0 0;color:#4b5563;font-size:13px;line-height:1.5;\">{safeHelperNote}</p>";

            return $"""
<!doctype html>
<html>
<head>
  <meta charset="UTF-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1.0" />
</head>
<body style="margin:0;background:#f3f5f9;font-family:Arial,Helvetica,sans-serif;color:#111827;">
  <div style="max-width:680px;margin:24px auto;padding:0 12px;">
    <div style="background:#ffffff;border:1px solid #e5e7eb;border-radius:14px;overflow:hidden;">
      <div style="background:linear-gradient(90deg,#1d4ed8,#1e40af);padding:20px 24px;color:#ffffff;">
        <div style="font-size:22px;font-weight:700;">ExamNest</div>
        <div style="font-size:13px;opacity:.9;margin-top:4px;">Learning notifications</div>
      </div>
      <div style="padding:24px;">
        {badgeHtml}
        <h1 style="margin:0 0 10px;font-size:24px;line-height:1.25;color:#0f172a;">{safeTitle}</h1>
        <p style="margin:0 0 18px;color:#374151;font-size:15px;line-height:1.6;">{safeSubtitle}</p>
        {highlightHtml}
        {detailsHtml}
        {actionHtml}
        {helperHtml}
      </div>
      <div style="padding:16px 24px;background:#f9fafb;border-top:1px solid #e5e7eb;color:#6b7280;font-size:12px;line-height:1.5;">
        This is an automated message from ExamNest. Please do not reply directly to this email.
      </div>
    </div>
  </div>
</body>
</html>
""";
        }

        private static string BuildDetailsTable(IReadOnlyDictionary<string, string>? detailsRows)
        {
            if (detailsRows == null || detailsRows.Count == 0)
                return string.Empty;

            var sb = new StringBuilder();
            sb.Append("<table role=\"presentation\" style=\"width:100%;border-collapse:collapse;margin:0 0 8px;\">");

            foreach (var row in detailsRows)
            {
                if (string.IsNullOrWhiteSpace(row.Key) || string.IsNullOrWhiteSpace(row.Value))
                    continue;

                sb.Append("<tr>");
                sb.Append($"<td style=\"padding:9px 10px;border:1px solid #e5e7eb;background:#f8fafc;font-size:13px;font-weight:700;color:#334155;width:38%;\">{Encode(row.Key)}</td>");
                sb.Append($"<td style=\"padding:9px 10px;border:1px solid #e5e7eb;font-size:13px;color:#0f172a;\">{Encode(row.Value)}</td>");
                sb.Append("</tr>");
            }

            sb.Append("</table>");
            return sb.ToString();
        }

        private static string Encode(string value) => WebUtility.HtmlEncode(value);
    }
}
