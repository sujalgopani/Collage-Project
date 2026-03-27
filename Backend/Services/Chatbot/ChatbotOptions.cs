namespace ExamNest.Services.Chatbot
{
    public class ChatbotOptions
    {
        public string PreferredProvider { get; set; } = "OpenRouterFree";
        public string OpenRouterBaseUrl { get; set; } = "https://openrouter.ai/api/v1";
        public string OpenRouterModel { get; set; } = "meta-llama/llama-3.2-3b-instruct:free";
        public string? OpenRouterApiKey { get; set; }
        public int OpenRouterTimeoutSeconds { get; set; } = 20;
        public bool EnableOllamaFallback { get; set; } = false;
        public int ProviderFailureCooldownSeconds { get; set; } = 60;

        public string OllamaBaseUrl { get; set; } = "http://127.0.0.1:11434";
        public string Model { get; set; } = "llama3.1:8b";
        public int TimeoutSeconds { get; set; } = 12;
        public double Temperature { get; set; } = 0.15;
        public int MaxContextItems { get; set; } = 4;
        public int MaxResponseTokens { get; set; } = 120;
        public int ContextCacheSeconds { get; set; } = 60;
        public int MaxHistoryMessages { get; set; } = 4;
        public int MaxHistoryContentLength { get; set; } = 350;
        public int AnswerCacheSeconds { get; set; } = 180;
    }
}
