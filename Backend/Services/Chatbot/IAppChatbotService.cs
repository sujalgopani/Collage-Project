using ExamNest.Models.DTOs.Chatbot;

namespace ExamNest.Services.Chatbot
{
    public interface IAppChatbotService
    {
        Task<ChatbotAskResponse> AskAsync(
            int userId,
            string role,
            ChatbotAskRequest request,
            CancellationToken cancellationToken = default);
    }
}
