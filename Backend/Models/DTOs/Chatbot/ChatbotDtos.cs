using System.ComponentModel.DataAnnotations;

namespace ExamNest.Models.DTOs.Chatbot
{
    public class ChatbotAskRequest
    {
        [Required]
        [StringLength(1000, MinimumLength = 2)]
        public string Message { get; set; } = string.Empty;

        public List<ChatbotHistoryItemDto>? History { get; set; }
    }

    public class ChatbotHistoryItemDto
    {
        [Required]
        [StringLength(20)]
        public string Role { get; set; } = "user";

        [Required]
        [StringLength(1000, MinimumLength = 1)]
        public string Content { get; set; } = string.Empty;
    }

    public class ChatbotAskResponse
    {
        public string Role { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public bool UsedFallback { get; set; }
        public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
