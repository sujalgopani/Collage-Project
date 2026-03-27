using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using ExamNest.Data;
using ExamNest.Models.DTOs.Chatbot;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace ExamNest.Services.Chatbot
{
    public class AppChatbotService : IAppChatbotService
    {
        private const int HistoryLimit = 6;
        private const int MaxUserMessageLength = 1000;
        private const int HistoryItemDefaultLength = 500;
        private const int NormalizedCacheMessageMaxLength = 160;
        private const string ProviderOpenRouter = "OpenRouterFree";
        private const string ProviderOllama = "Ollama";

        private readonly AppDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _memoryCache;
        private readonly ChatbotOptions _options;
        private readonly ILogger<AppChatbotService> _logger;

        public AppChatbotService(
            AppDbContext context,
            IHttpClientFactory httpClientFactory,
            IMemoryCache memoryCache,
            IOptions<ChatbotOptions> options,
            ILogger<AppChatbotService> logger)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _memoryCache = memoryCache;
            _options = options.Value ?? new ChatbotOptions();
            _logger = logger;
        }

        public async Task<ChatbotAskResponse> AskAsync(
            int userId,
            string role,
            ChatbotAskRequest request,
            CancellationToken cancellationToken = default)
        {
            var message = Truncate(request.Message?.Trim() ?? string.Empty, MaxUserMessageLength);
            if (string.IsNullOrWhiteSpace(message))
            {
                return new ChatbotAskResponse
                {
                    Role = NormalizeRole(role),
                    Answer = "Please type your question so I can help.",
                    UsedFallback = true,
                    GeneratedAtUtc = DateTime.UtcNow
                };
            }

            var normalizedRole = NormalizeRole(role);
            var normalizedCacheMessage = NormalizeMessageForCache(message);
            var answerCacheKey = BuildAnswerCacheKey(userId, normalizedRole, normalizedCacheMessage);
            if (_memoryCache.TryGetValue<ChatbotAskResponse>(answerCacheKey, out var cachedResponse) && cachedResponse != null)
            {
                return CloneCachedResponse(cachedResponse);
            }

            var roleContext = await GetOrCreateRoleContextAsync(userId, normalizedRole, cancellationToken);

            if (TryBuildProjectFlowAnswer(normalizedRole, message, roleContext, out var flowAnswer))
            {
                var fastFlowResponse = new ChatbotAskResponse
                {
                    Role = normalizedRole,
                    Answer = flowAnswer,
                    UsedFallback = false,
                    GeneratedAtUtc = DateTime.UtcNow
                };

                CacheAnswer(answerCacheKey, fastFlowResponse);
                return fastFlowResponse;
            }

            if (TryBuildCountAnswer(message, roleContext, out var countAnswer))
            {
                var countResponse = new ChatbotAskResponse
                {
                    Role = normalizedRole,
                    Answer = countAnswer,
                    UsedFallback = false,
                    GeneratedAtUtc = DateTime.UtcNow
                };

                CacheAnswer(answerCacheKey, countResponse);
                return countResponse;
            }

            if (TryBuildInstantReply(normalizedRole, message, roleContext, out var instantReply))
            {
                var instantResponse = new ChatbotAskResponse
                {
                    Role = normalizedRole,
                    Answer = instantReply,
                    UsedFallback = false,
                    GeneratedAtUtc = DateTime.UtcNow
                };

                CacheAnswer(answerCacheKey, instantResponse);
                return instantResponse;
            }

            var chatMessages = BuildChatMessages(message, normalizedRole, request.History, roleContext);
            var provider = NormalizeProvider(_options.PreferredProvider);
            if (IsProviderTemporarilyUnavailable(provider))
            {
                var cooldownSeconds = ResolveProviderFailureCooldownSeconds();
                var reason = provider == ProviderOllama
                    ? $"Local AI is temporarily paused after a recent failure. It will retry in about {cooldownSeconds} seconds."
                    : $"Online AI is temporarily paused after a recent provider failure. It will retry in about {cooldownSeconds} seconds.";

                var localModeResponse = BuildFallbackResponse(normalizedRole, message, roleContext, reason);
                CacheAnswer(answerCacheKey, localModeResponse);
                return localModeResponse;
            }

            ProviderResult providerResult;
            if (provider == ProviderOllama)
            {
                providerResult = await TryAskOllamaAsync(chatMessages, cancellationToken);
            }
            else
            {
                providerResult = await TryAskOpenRouterAsync(chatMessages, cancellationToken);
                if (!providerResult.Success && _options.EnableOllamaFallback)
                {
                    var fallbackResult = await TryAskOllamaAsync(chatMessages, cancellationToken);
                    if (fallbackResult.Success)
                    {
                        providerResult = fallbackResult;
                    }
                }
            }

            if (providerResult.Success)
            {
                var llmResponse = new ChatbotAskResponse
                {
                    Role = normalizedRole,
                    Answer = providerResult.Answer,
                    UsedFallback = false,
                    GeneratedAtUtc = DateTime.UtcNow
                };

                CacheAnswer(answerCacheKey, llmResponse);
                return llmResponse;
            }

            if (providerResult.ApplyCooldown)
            {
                MarkProviderTemporarilyUnavailable(provider);
            }

            _logger.LogWarning(
                "Chatbot provider {Provider} failed. Reason: {Reason}",
                provider,
                providerResult.ErrorReason);

            var fallbackResponse = BuildFallbackResponse(normalizedRole, message, roleContext, providerResult.ErrorReason);
            CacheAnswer(answerCacheKey, fallbackResponse);
            return fallbackResponse;
        }

        private async Task<RoleContext> GetOrCreateRoleContextAsync(
            int userId,
            string role,
            CancellationToken cancellationToken)
        {
            var cacheSeconds = _options.ContextCacheSeconds;
            if (cacheSeconds <= 0)
            {
                return await BuildRoleContextAsync(userId, role, cancellationToken);
            }

            var cacheKey = role == "Admin"
                ? "chatbot:context:admin"
                : $"chatbot:context:{role}:{userId}";

            if (_memoryCache.TryGetValue<RoleContext>(cacheKey, out var cachedContext) && cachedContext != null)
            {
                return cachedContext;
            }

            var roleContext = await BuildRoleContextAsync(userId, role, cancellationToken);
            _memoryCache.Set(cacheKey, roleContext, TimeSpan.FromSeconds(cacheSeconds));
            return roleContext;
        }

        private async Task<RoleContext> BuildRoleContextAsync(int userId, string role, CancellationToken cancellationToken)
        {
            return role switch
            {
                "Teacher" => await BuildTeacherContextAsync(userId, cancellationToken),
                "Admin" => await BuildAdminContextAsync(cancellationToken),
                _ => await BuildStudentContextAsync(userId, cancellationToken),
            };
        }

        private async Task<RoleContext> BuildStudentContextAsync(int userId, CancellationToken cancellationToken)
        {
            var roleContext = new RoleContext
            {
                Role = "Student",
                FeatureHints = GetFeatureHints("Student"),
            };

            var now = DateTime.UtcNow;
            var contextItems = ResolveContextItemLimit();

            var activeSubscriptions = await _context.Subscriptions
                .AsNoTracking()
                .Where(s => s.StudentId == userId && s.Status == "Active")
                .Select(s => new
                {
                    s.CourseId,
                    CourseTitle = s.Course != null ? s.Course.Title : "Course"
                })
                .ToListAsync(cancellationToken);

            var courseIds = activeSubscriptions
                .Select(s => s.CourseId)
                .Distinct()
                .ToList();

            var courseTitles = activeSubscriptions
                .Select(s => s.CourseTitle)
                .Distinct()
                .Take(contextItems)
                .ToList();

            roleContext.DynamicFacts.Add($"Active subscriptions: {courseIds.Count}.");
            if (courseTitles.Count > 0)
            {
                roleContext.DynamicFacts.Add($"Current courses: {string.Join(", ", courseTitles)}.");
            }

            var upcomingExams = await _context.Exams
                .AsNoTracking()
                .Where(e => courseIds.Contains(e.CourseId) && e.EndAt >= now)
                .OrderBy(e => e.StartAt)
                .Select(e => new { e.Title, e.StartAt })
                .Take(contextItems)
                .ToListAsync(cancellationToken);

            roleContext.DynamicFacts.Add($"Upcoming/active exams: {upcomingExams.Count}.");
            if (upcomingExams.Count > 0)
            {
                var exams = upcomingExams
                    .Select(e => $"{e.Title} ({FormatUtcToLocal(e.StartAt)})");
                roleContext.DynamicFacts.Add($"Exam schedule snapshot: {string.Join("; ", exams)}.");
            }

            var upcomingLiveClasses = await _context.LiveClassSchedules
                .AsNoTracking()
                .Where(x => courseIds.Contains(x.CourseId) && !x.IsCancelled && x.EndAt >= now)
                .OrderBy(x => x.StartAt)
                .Select(x => new
                {
                    x.Title,
                    x.StartAt,
                    CourseTitle = x.Course != null ? x.Course.Title : "Course"
                })
                .Take(contextItems)
                .ToListAsync(cancellationToken);

            roleContext.DynamicFacts.Add($"Upcoming live classes: {upcomingLiveClasses.Count}.");
            if (upcomingLiveClasses.Count > 0)
            {
                var classes = upcomingLiveClasses
                    .Select(x => $"{x.Title} in {x.CourseTitle} ({FormatUtcToLocal(x.StartAt)})");
                roleContext.DynamicFacts.Add($"Live class snapshot: {string.Join("; ", classes)}.");
            }

            var attemptedExams = await _context.ExamAttempts
                .AsNoTracking()
                .Where(a => a.StudentId == userId && a.SubmittedAt != null)
                .CountAsync(cancellationToken);

            roleContext.DynamicFacts.Add($"Submitted exam attempts: {attemptedExams}.");

            var scoreRows = await _context.ExamAttempts
                .AsNoTracking()
                .Where(a => a.StudentId == userId && a.SubmittedAt != null && a.MaxScore > 0)
                .Select(a => new { a.TotalScore, a.MaxScore })
                .ToListAsync(cancellationToken);

            if (scoreRows.Count > 0)
            {
                var avgScore = scoreRows.Average(x => (double)x.TotalScore / x.MaxScore * 100d);
                roleContext.DynamicFacts.Add($"Average exam score: {Math.Round(avgScore, 2)}%.");
            }

            var openSuggestions = await _context.Suggestions
                .AsNoTracking()
                .Where(s => s.StudentId == userId && (s.Status == null || s.Status != "Resolved"))
                .CountAsync(cancellationToken);

            roleContext.DynamicFacts.Add($"Open suggestions: {openSuggestions}.");

            return roleContext;
        }

        private async Task<RoleContext> BuildTeacherContextAsync(int userId, CancellationToken cancellationToken)
        {
            var roleContext = new RoleContext
            {
                Role = "Teacher",
                FeatureHints = GetFeatureHints("Teacher"),
            };

            var now = DateTime.UtcNow;
            var contextItems = ResolveContextItemLimit();

            var myCourses = await _context.Courses
                .AsNoTracking()
                .Where(c => c.TeacherId == userId)
                .Select(c => new { c.CourseId, c.Title, c.IsPublished })
                .ToListAsync(cancellationToken);

            var courseIds = myCourses.Select(c => c.CourseId).ToList();
            var publishedCourseCount = myCourses.Count(c => c.IsPublished);

            roleContext.DynamicFacts.Add($"Your courses: {myCourses.Count} total, {publishedCourseCount} published.");
            if (myCourses.Count > 0)
            {
                roleContext.DynamicFacts.Add($"Course snapshot: {string.Join(", ", myCourses.Take(contextItems).Select(c => c.Title))}.");
            }

            var activeStudents = await _context.Subscriptions
                .AsNoTracking()
                .Where(s => courseIds.Contains(s.CourseId) && s.Status == "Active")
                .Select(s => s.StudentId)
                .Distinct()
                .CountAsync(cancellationToken);

            roleContext.DynamicFacts.Add($"Active students across your courses: {activeStudents}.");

            var upcomingExams = await _context.Exams
                .AsNoTracking()
                .Where(e => e.TeacherId == userId && e.EndAt >= now)
                .OrderBy(e => e.StartAt)
                .Select(e => new { e.Title, e.StartAt })
                .Take(contextItems)
                .ToListAsync(cancellationToken);

            roleContext.DynamicFacts.Add($"Upcoming/active exams: {upcomingExams.Count}.");
            if (upcomingExams.Count > 0)
            {
                roleContext.DynamicFacts.Add(
                    $"Exam schedule snapshot: {string.Join("; ", upcomingExams.Select(e => $"{e.Title} ({FormatUtcToLocal(e.StartAt)})"))}.");
            }

            var upcomingLiveClasses = await _context.LiveClassSchedules
                .AsNoTracking()
                .Where(x => x.TeacherId == userId && !x.IsCancelled && x.EndAt >= now)
                .OrderBy(x => x.StartAt)
                .Select(x => new { x.Title, x.StartAt })
                .Take(contextItems)
                .ToListAsync(cancellationToken);

            roleContext.DynamicFacts.Add($"Upcoming live classes: {upcomingLiveClasses.Count}.");
            if (upcomingLiveClasses.Count > 0)
            {
                roleContext.DynamicFacts.Add(
                    $"Live class snapshot: {string.Join("; ", upcomingLiveClasses.Select(x => $"{x.Title} ({FormatUtcToLocal(x.StartAt)})"))}.");
            }

            var openSuggestions = await _context.Suggestions
                .AsNoTracking()
                .Where(s => s.TeacherId == userId && (s.Status == null || s.Status != "Resolved"))
                .CountAsync(cancellationToken);

            roleContext.DynamicFacts.Add($"Pending student suggestions to reply: {openSuggestions}.");

            return roleContext;
        }

        private async Task<RoleContext> BuildAdminContextAsync(CancellationToken cancellationToken)
        {
            var roleContext = new RoleContext
            {
                Role = "Admin",
                FeatureHints = GetFeatureHints("Admin"),
            };

            var now = DateTime.UtcNow;

            var studentCount = await _context.Users.AsNoTracking().CountAsync(u => u.RoleId == 3, cancellationToken);
            var teacherCount = await _context.Users.AsNoTracking().CountAsync(u => u.RoleId == 2, cancellationToken);
            var courseCount = await _context.Courses.AsNoTracking().CountAsync(cancellationToken);
            var publishedCourseCount = await _context.Courses.AsNoTracking().CountAsync(c => c.IsPublished, cancellationToken);
            var examCount = await _context.Exams.AsNoTracking().CountAsync(cancellationToken);
            var upcomingLiveClasses = await _context.LiveClassSchedules
                .AsNoTracking()
                .CountAsync(x => !x.IsCancelled && x.EndAt >= now, cancellationToken);
            var openSuggestions = await _context.Suggestions
                .AsNoTracking()
                .CountAsync(s => s.Status == null || s.Status != "Resolved", cancellationToken);

            roleContext.DynamicFacts.Add($"Users: {studentCount} students and {teacherCount} teachers.");
            roleContext.DynamicFacts.Add($"Courses: {courseCount} total, {publishedCourseCount} published.");
            roleContext.DynamicFacts.Add($"Exams in system: {examCount}.");
            roleContext.DynamicFacts.Add($"Upcoming live classes (all courses): {upcomingLiveClasses}.");
            roleContext.DynamicFacts.Add($"Open suggestions platform-wide: {openSuggestions}.");

            return roleContext;
        }

        private List<OllamaMessage> BuildChatMessages(
            string userMessage,
            string role,
            List<ChatbotHistoryItemDto>? history,
            RoleContext roleContext)
        {
            var messages = new List<OllamaMessage>
            {
                new()
                {
                    Role = "system",
                    Content = BuildSystemPrompt(role),
                },
                new()
                {
                    Role = "system",
                    Content = BuildRoleContextPrompt(roleContext),
                }
            };

            var historyLimit = ResolveHistoryMessageLimit();
            var historyContentLength = ResolveHistoryContentLength();
            var sanitizedHistory = (history ?? new List<ChatbotHistoryItemDto>())
                .TakeLast(historyLimit)
                .Select(h => new
                {
                    Role = NormalizeHistoryRole(h.Role),
                    Content = Truncate(h.Content?.Trim() ?? string.Empty, historyContentLength)
                })
                .Where(h => h.Role != null && !string.IsNullOrWhiteSpace(h.Content));

            foreach (var item in sanitizedHistory)
            {
                messages.Add(new OllamaMessage
                {
                    Role = item.Role!,
                    Content = item.Content
                });
            }

            messages.Add(new OllamaMessage
            {
                Role = "user",
                Content = userMessage
            });

            return messages;
        }

        private async Task<ProviderResult> TryAskOpenRouterAsync(
            List<OllamaMessage> chatMessages,
            CancellationToken cancellationToken)
        {
            var apiKey = ResolveOpenRouterApiKey();
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return ProviderResult.FromFailure("Online free AI key is not configured.", applyCooldown: false);
            }

            var request = new OpenRouterChatRequest
            {
                Model = string.IsNullOrWhiteSpace(_options.OpenRouterModel)
                    ? "meta-llama/llama-3.2-3b-instruct:free"
                    : _options.OpenRouterModel.Trim(),
                Temperature = _options.Temperature,
                MaxTokens = ResolveMaxResponseTokens(),
                Stream = false,
                Messages = chatMessages.Select(x => new OpenRouterMessage
                {
                    Role = x.Role,
                    Content = x.Content
                }).ToList()
            };

            try
            {
                var client = _httpClientFactory.CreateClient("OpenRouterChat");
                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
                {
                    Content = JsonContent.Create(request)
                };

                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                httpRequest.Headers.TryAddWithoutValidation("HTTP-Referer", "https://examnest.local");
                httpRequest.Headers.TryAddWithoutValidation("X-Title", "ExamNest Assistant");

                using var response = await client.SendAsync(httpRequest, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(cancellationToken);
                    var status = (int)response.StatusCode;

                    _logger.LogWarning(
                        "OpenRouter request failed with status code {StatusCode}. Body: {Body}",
                        response.StatusCode,
                        body);

                    if (status == 401 || status == 403)
                    {
                        return ProviderResult.FromFailure(
                            "Online free AI key is invalid or unauthorized.",
                            applyCooldown: false);
                    }

                    if (status == 429)
                    {
                        return ProviderResult.FromFailure(
                            "OpenRouter model is currently rate-limited. Retry shortly or switch Chatbot:OpenRouterModel.",
                            applyCooldown: true);
                    }

                    if (status == 404)
                    {
                        return ProviderResult.FromFailure(
                            "No OpenRouter endpoint is available for the configured model/account policy.",
                            applyCooldown: false);
                    }

                    if (status == 400 && body.Contains("model", StringComparison.OrdinalIgnoreCase))
                    {
                        return ProviderResult.FromFailure(
                            "Configured OpenRouter model is unavailable. Use a valid ':free' model.",
                            applyCooldown: false);
                    }

                    return ProviderResult.FromFailure("Online free AI service is unavailable right now.");
                }

                var payload = await response.Content.ReadFromJsonAsync<OpenRouterChatResponse>(cancellationToken: cancellationToken);
                var answer = payload?.Choices?.FirstOrDefault()?.Message?.Content?.Trim();
                if (string.IsNullOrWhiteSpace(answer))
                {
                    return ProviderResult.FromFailure("Online free AI returned an empty response.");
                }

                return ProviderResult.FromSuccess(answer);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "OpenRouter request failed.");
                return ProviderResult.FromFailure("Online free AI service is unreachable.");
            }
        }

        private async Task<ProviderResult> TryAskOllamaAsync(
            List<OllamaMessage> chatMessages,
            CancellationToken cancellationToken)
        {
            var ollamaRequest = new OllamaChatRequest
            {
                Model = string.IsNullOrWhiteSpace(_options.Model) ? "llama3.1:8b" : _options.Model.Trim(),
                Stream = false,
                Messages = chatMessages,
                Options = new OllamaOptions
                {
                    Temperature = _options.Temperature,
                    NumPredict = ResolveMaxResponseTokens(),
                    NumContextWindow = 2048
                }
            };

            try
            {
                var client = _httpClientFactory.CreateClient("OllamaChat");
                using var response = await client.PostAsJsonAsync("api/chat", ollamaRequest, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning(
                        "Ollama request failed with status code {StatusCode}. Body: {Body}",
                        response.StatusCode,
                        body);

                    return ProviderResult.FromFailure("Local Ollama service is unavailable right now.");
                }

                var payload = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(cancellationToken: cancellationToken);
                var answer = payload?.Message?.Content?.Trim();
                if (string.IsNullOrWhiteSpace(answer))
                {
                    return ProviderResult.FromFailure("Local model returned an empty response.", applyCooldown: false);
                }

                return ProviderResult.FromSuccess(answer);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Ollama request failed.");
                return ProviderResult.FromFailure("Local Ollama service is unreachable.");
            }
        }

        private string? ResolveOpenRouterApiKey()
        {
            if (!string.IsNullOrWhiteSpace(_options.OpenRouterApiKey))
            {
                return _options.OpenRouterApiKey.Trim();
            }

            var env = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");
            return string.IsNullOrWhiteSpace(env) ? null : env.Trim();
        }

        private ChatbotAskResponse BuildFallbackResponse(
            string role,
            string message,
            RoleContext roleContext,
            string reason)
        {
            var lines = new List<string>
            {
                reason,
                "I can still help with live panel data while the model is unavailable.",
                string.Empty,
                "Live snapshot:"
            };

            lines.AddRange(roleContext.DynamicFacts.Take(4).Select(f => $"- {f}"));

            var routeHint = GetRouteHint(role, message);
            if (!string.IsNullOrWhiteSpace(routeHint))
            {
                lines.Add(string.Empty);
                lines.Add($"You can check this directly in app: {routeHint}");
            }

            lines.Add(string.Empty);
            if (ContainsAny(reason, "key", "unauthorized", "api key", "not configured"))
            {
                lines.Add("Set Chatbot:OpenRouterApiKey (or OPENROUTER_API_KEY) and restart backend.");
            }
            else if (ContainsAny(reason, "rate-limit", "rate limit", "unavailable", "unreachable"))
            {
                lines.Add("Provider is busy/unreachable. Retry shortly, or change Chatbot:OpenRouterModel.");
            }
            else if (ContainsAny(reason, "model", "endpoint"))
            {
                lines.Add("Update Chatbot:OpenRouterModel to an available model and restart backend.");
            }
            else
            {
                lines.Add("Check chatbot provider settings in backend and retry.");
            }

            return new ChatbotAskResponse
            {
                Role = role,
                Answer = string.Join(Environment.NewLine, lines).Trim(),
                UsedFallback = true,
                GeneratedAtUtc = DateTime.UtcNow
            };
        }

        private ChatbotAskResponse BuildLocalModeResponse(
            string role,
            string message,
            RoleContext roleContext)
        {
            var routeHint = GetRouteHint(role, message);
            var lines = new List<string>
            {
                "Fast assistant mode is active for now.",
                $"Best route for this task: `{routeHint}`",
                string.Empty,
                "Current snapshot:"
            };

            lines.AddRange(roleContext.DynamicFacts.Take(4).Select(f => $"- {f}"));

            lines.Add(string.Empty);
            lines.Add("Ask: \"show full project flow\" for complete step-by-step routes.");
            lines.Add("For online AI answers, configure OPENROUTER_API_KEY in backend.");

            return new ChatbotAskResponse
            {
                Role = role,
                Answer = string.Join(Environment.NewLine, lines).Trim(),
                UsedFallback = true,
                GeneratedAtUtc = DateTime.UtcNow
            };
        }

        private static string BuildSystemPrompt(string role)
        {
            return $"""
                    You are ExamNest Assistant for a college management app.
                    Current user role: {role}.

                    Rules:
                    1) Answer only questions related to ExamNest app features, workflows, and available data.
                    2) Keep replies concise (3-6 lines), practical, and step-by-step when needed.
                    2.1) For workflow/process questions, include route flow using arrow format: step -> route.
                    3) If the question needs another role's panel permissions, explain that clearly and suggest the right panel path.
                    4) Do not invent data. Use only provided context and user message.
                    5) If something is unavailable, state it and suggest what to check next.
                    """;
        }

        private static string BuildRoleContextPrompt(RoleContext roleContext)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Role: {roleContext.Role}");
            sb.AppendLine("Primary panel routes:");
            foreach (var route in GetPrimaryPanelRoutes(roleContext.Role))
            {
                sb.AppendLine($"- {route}");
            }

            sb.AppendLine("Live user context:");
            if (roleContext.DynamicFacts.Count == 0)
            {
                sb.AppendLine("- No live role data available.");
            }
            else
            {
                foreach (var fact in roleContext.DynamicFacts.Take(6))
                {
                    sb.AppendLine($"- {fact}");
                }
            }

            return sb.ToString();
        }

        private static List<string> GetPrimaryPanelRoutes(string role)
        {
            return role switch
            {
                "Teacher" => new List<string>
                {
                    "/teacher-dashboard/your-course",
                    "/teacher-dashboard/exam-list",
                    "/teacher-dashboard/live-classes",
                    "/teacher-dashboard/teachersuggestion"
                },
                "Admin" => new List<string>
                {
                    "/admin-dashboard/course-manage",
                    "/admin-dashboard/exams-manage",
                    "/admin-dashboard/payment-manage",
                    "/admin-dashboard/live-classes"
                },
                _ => new List<string>
                {
                    "/student-dashboard/published-courses",
                    "/student-dashboard/student-exam",
                    "/student-dashboard/live-classes",
                    "/student-dashboard/student-exam-result"
                },
            };
        }

        private static List<string> GetFeatureHints(string role)
        {
            return role switch
            {
                "Teacher" => new List<string>
                {
                    "Teacher dashboard home: /teacher-dashboard",
                    "Create course: /teacher-dashboard/create-couse",
                    "Your courses: /teacher-dashboard/your-course",
                    "Course students: /teacher-dashboard/studentcoursewise",
                    "Live materials/classes: /teacher-dashboard/live-classes",
                    "Exam list: /teacher-dashboard/exam-list",
                    "Exam students: /teacher-dashboard/studentexamwise",
                    "Teacher suggestions: /teacher-dashboard/teachersuggestion",
                    "Profile settings: /teacher-dashboard/teacherprofile",
                },
                "Admin" => new List<string>
                {
                    "Admin dashboard home: /admin-dashboard",
                    "Teachers management: /admin-dashboard/main-teacher",
                    "Students management: /admin-dashboard/main-student",
                    "Course management: /admin-dashboard/course-manage",
                    "Exam management: /admin-dashboard/exams-manage",
                    "Payment management: /admin-dashboard/payment-manage",
                    "Live classes management: /admin-dashboard/live-classes",
                    "Role management: /admin-dashboard/role-manage",
                    "Profile settings: /admin-dashboard/adminprofile",
                },
                _ => new List<string>
                {
                    "Student dashboard home: /student-dashboard",
                    "Explore courses: /student-dashboard/published-courses",
                    "My learning: /student-dashboard/learn-courses",
                    "Live classes: /student-dashboard/live-classes",
                    "Exams: /student-dashboard/student-exam",
                    "Exam attempt: /student-dashboard/student-exam-attempt",
                    "Exam results: /student-dashboard/student-exam-result",
                    "Send suggestion: /student-dashboard/studentsuggestion",
                    "View suggestion replies: /student-dashboard/mysuggestion",
                    "Profile settings: /student-dashboard/studentprofile",
                },
            };
        }

        private static bool TryBuildProjectFlowAnswer(
            string role,
            string message,
            RoleContext roleContext,
            out string answer)
        {
            answer = string.Empty;
            var lower = message.ToLowerInvariant();
            var asksFlow = ContainsAny(lower, "flow", "workflow", "process", "journey", "steps");
            if (!asksFlow)
            {
                return false;
            }

            var wantsFullFlow = ContainsAny(lower, "full", "complete", "overall", "entire", "all", "project");
            if (wantsFullFlow)
            {
                answer = BuildFullFlowAnswer(role, roleContext);
                return true;
            }

            var module = ResolveFlowModule(lower);
            if (string.IsNullOrWhiteSpace(module))
            {
                answer = BuildFullFlowAnswer(role, roleContext);
                return true;
            }

            answer = BuildModuleFlowAnswer(role, module, roleContext);
            return true;
        }

        private static bool TryBuildCountAnswer(string message, RoleContext roleContext, out string answer)
        {
            answer = string.Empty;
            var lower = message.ToLowerInvariant();
            if (!ContainsAny(lower, "how many", "count", "total"))
            {
                return false;
            }

            var module = ResolveFlowModule(lower) ?? string.Empty;
            string[] keywords = module switch
            {
                "courses" => new[] { "course" },
                "exams" => new[] { "exam" },
                "live classes" => new[] { "live class" },
                "suggestions" => new[] { "suggestion" },
                "payments" => new[] { "payment", "earning" },
                "users" => new[] { "student", "teacher", "user" },
                _ => new[] { "active", "total", "upcoming" },
            };

            var fact = roleContext.DynamicFacts.FirstOrDefault(f =>
                keywords.Any(k => f.Contains(k, StringComparison.OrdinalIgnoreCase)));

            if (string.IsNullOrWhiteSpace(fact))
            {
                return false;
            }

            answer = $"{fact}{Environment.NewLine}If you want workflow steps too, ask: \"show full project flow\".";
            return true;
        }

        private static string BuildFullFlowAnswer(string role, RoleContext roleContext)
        {
            var steps = GetRoleFlowSteps(role);
            var lines = new List<string>
            {
                $"{role} panel full project flow:"
            };

            for (var i = 0; i < steps.Count; i++)
            {
                lines.Add($"{i + 1}. {steps[i].Label} -> `{steps[i].Route}`");
            }

            if (roleContext.DynamicFacts.Count > 0)
            {
                lines.Add(string.Empty);
                lines.Add("Current snapshot:");
                lines.AddRange(roleContext.DynamicFacts.Take(3).Select(x => $"- {x}"));
            }

            return string.Join(Environment.NewLine, lines);
        }

        private static string BuildModuleFlowAnswer(string role, string module, RoleContext roleContext)
        {
            var moduleSteps = GetRoleFlowSteps(role)
                .Where(x => string.Equals(x.Module, module, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (moduleSteps.Count == 0)
            {
                return BuildFullFlowAnswer(role, roleContext);
            }

            var lines = new List<string>
            {
                $"{role} panel {module} flow:"
            };

            for (var i = 0; i < moduleSteps.Count; i++)
            {
                lines.Add($"{i + 1}. {moduleSteps[i].Label} -> `{moduleSteps[i].Route}`");
            }

            var relatedFact = roleContext.DynamicFacts.FirstOrDefault(f =>
                module switch
                {
                    "courses" => f.Contains("course", StringComparison.OrdinalIgnoreCase),
                    "exams" => f.Contains("exam", StringComparison.OrdinalIgnoreCase),
                    "live classes" => f.Contains("live class", StringComparison.OrdinalIgnoreCase),
                    "suggestions" => f.Contains("suggestion", StringComparison.OrdinalIgnoreCase),
                    "payments" => f.Contains("earning", StringComparison.OrdinalIgnoreCase) || f.Contains("payment", StringComparison.OrdinalIgnoreCase),
                    "users" => f.Contains("student", StringComparison.OrdinalIgnoreCase) || f.Contains("teacher", StringComparison.OrdinalIgnoreCase),
                    _ => false
                });

            if (!string.IsNullOrWhiteSpace(relatedFact))
            {
                lines.Add(string.Empty);
                lines.Add($"Current status: {relatedFact}");
            }

            return string.Join(Environment.NewLine, lines);
        }

        private static List<(string Module, string Label, string Route)> GetRoleFlowSteps(string role)
        {
            return role switch
            {
                "Teacher" => new List<(string, string, string)>
                {
                    ("dashboard", "Open teacher dashboard", "/teacher-dashboard"),
                    ("courses", "Create a course", "/teacher-dashboard/create-couse"),
                    ("courses", "Manage your courses", "/teacher-dashboard/your-course"),
                    ("users", "See course students", "/teacher-dashboard/studentcoursewise"),
                    ("exams", "Create/manage exams", "/teacher-dashboard/exam-list"),
                    ("exams", "Check exam-wise students", "/teacher-dashboard/studentexamwise"),
                    ("live classes", "Share live class materials", "/teacher-dashboard/live-classes"),
                    ("suggestions", "Review and reply suggestions", "/teacher-dashboard/teachersuggestion"),
                    ("profile", "Update profile", "/teacher-dashboard/teacherprofile")
                },
                "Admin" => new List<(string, string, string)>
                {
                    ("dashboard", "Open admin dashboard", "/admin-dashboard"),
                    ("users", "Manage teachers", "/admin-dashboard/main-teacher"),
                    ("users", "Manage students", "/admin-dashboard/main-student"),
                    ("courses", "Review and publish courses", "/admin-dashboard/course-manage"),
                    ("exams", "Manage exams", "/admin-dashboard/exams-manage"),
                    ("payments", "Check payments", "/admin-dashboard/payment-manage"),
                    ("live classes", "Schedule/manage live classes", "/admin-dashboard/live-classes"),
                    ("roles", "Manage roles", "/admin-dashboard/role-manage"),
                    ("profile", "Update profile", "/admin-dashboard/adminprofile")
                },
                _ => new List<(string, string, string)>
                {
                    ("dashboard", "Open student dashboard", "/student-dashboard"),
                    ("courses", "Explore published courses", "/student-dashboard/published-courses"),
                    ("courses", "Access purchased learning", "/student-dashboard/learn-courses"),
                    ("live classes", "Join live classes", "/student-dashboard/live-classes"),
                    ("exams", "View available exams", "/student-dashboard/student-exam"),
                    ("exams", "Attempt an exam", "/student-dashboard/student-exam-attempt"),
                    ("exams", "Check results", "/student-dashboard/student-exam-result"),
                    ("suggestions", "Send suggestion", "/student-dashboard/studentsuggestion"),
                    ("suggestions", "Check teacher replies", "/student-dashboard/mysuggestion"),
                    ("profile", "Update profile", "/student-dashboard/studentprofile")
                },
            };
        }

        private static string? ResolveFlowModule(string lowerMessage)
        {
            if (lowerMessage.Contains("course")) return "courses";
            if (lowerMessage.Contains("exam")) return "exams";
            if (lowerMessage.Contains("live") || lowerMessage.Contains("material")) return "live classes";
            if (lowerMessage.Contains("suggestion") || lowerMessage.Contains("feedback")) return "suggestions";
            if (lowerMessage.Contains("profile")) return "profile";
            if (lowerMessage.Contains("payment") || lowerMessage.Contains("earning")) return "payments";
            if (lowerMessage.Contains("teacher") || lowerMessage.Contains("student") || lowerMessage.Contains("user")) return "users";
            if (lowerMessage.Contains("role")) return "roles";
            return null;
        }

        private static bool ContainsAny(string text, params string[] keywords)
        {
            return keywords.Any(k => text.Contains(k, StringComparison.OrdinalIgnoreCase));
        }

        private static bool TryBuildInstantReply(
            string role,
            string message,
            RoleContext roleContext,
            out string answer)
        {
            answer = string.Empty;
            var lower = message.ToLowerInvariant();
            var isNavigationQuery =
                lower.Contains("where") ||
                lower.Contains("route") ||
                lower.Contains("path") ||
                lower.Contains("which page") ||
                lower.Contains("open");

            if (!isNavigationQuery)
            {
                return false;
            }

            var route = GetRouteHint(role, message);
            var topic = "dashboard";
            if (lower.Contains("exam")) topic = "exams";
            else if (lower.Contains("course")) topic = "courses";
            else if (lower.Contains("live")) topic = "live classes";
            else if (lower.Contains("suggestion") || lower.Contains("feedback")) topic = "suggestions";
            else if (lower.Contains("profile")) topic = "profile";

            var firstFact = roleContext.DynamicFacts.FirstOrDefault();
            answer = $"Open `{route}` for {topic} in your {role.ToLowerInvariant()} panel." +
                     (string.IsNullOrWhiteSpace(firstFact) ? string.Empty : $"{Environment.NewLine}Quick status: {firstFact}");
            return true;
        }

        private static string GetRouteHint(string role, string message)
        {
            var lower = message.ToLowerInvariant();

            if (lower.Contains("exam"))
            {
                return role switch
                {
                    "Teacher" => "/teacher-dashboard/exam-list",
                    "Admin" => "/admin-dashboard/exams-manage",
                    _ => "/student-dashboard/student-exam",
                };
            }

            if (lower.Contains("course"))
            {
                return role switch
                {
                    "Teacher" => "/teacher-dashboard/your-course",
                    "Admin" => "/admin-dashboard/course-manage",
                    _ => "/student-dashboard/published-courses",
                };
            }

            if (lower.Contains("live class") || lower.Contains("live") || lower.Contains("material"))
            {
                return role switch
                {
                    "Teacher" => "/teacher-dashboard/live-classes",
                    "Admin" => "/admin-dashboard/live-classes",
                    _ => "/student-dashboard/live-classes",
                };
            }

            if (lower.Contains("suggestion") || lower.Contains("feedback"))
            {
                return role switch
                {
                    "Teacher" => "/teacher-dashboard/teachersuggestion",
                    "Admin" => "/admin-dashboard",
                    _ => "/student-dashboard/studentsuggestion",
                };
            }

            if (lower.Contains("profile"))
            {
                return role switch
                {
                    "Teacher" => "/teacher-dashboard/teacherprofile",
                    "Admin" => "/admin-dashboard/adminprofile",
                    _ => "/student-dashboard/studentprofile",
                };
            }

            return role switch
            {
                "Teacher" => "/teacher-dashboard",
                "Admin" => "/admin-dashboard",
                _ => "/student-dashboard",
            };
        }

        private int ResolveContextItemLimit()
        {
            return _options.MaxContextItems <= 0 ? 5 : _options.MaxContextItems;
        }

        private int ResolveMaxResponseTokens()
        {
            return _options.MaxResponseTokens <= 0 ? 180 : _options.MaxResponseTokens;
        }

        private int ResolveHistoryMessageLimit()
        {
            return _options.MaxHistoryMessages <= 0 ? HistoryLimit : _options.MaxHistoryMessages;
        }

        private int ResolveHistoryContentLength()
        {
            return _options.MaxHistoryContentLength <= 0 ? HistoryItemDefaultLength : _options.MaxHistoryContentLength;
        }

        private int ResolveAnswerCacheSeconds()
        {
            return _options.AnswerCacheSeconds <= 0 ? 180 : _options.AnswerCacheSeconds;
        }

        private static string BuildAnswerCacheKey(int userId, string role, string normalizedMessage)
        {
            return $"chatbot:answer:{role}:{userId}:{normalizedMessage}";
        }

        private static string NormalizeMessageForCache(string message)
        {
            var normalized = string.Join(
                ' ',
                message
                    .ToLowerInvariant()
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries));

            if (normalized.Length <= NormalizedCacheMessageMaxLength)
            {
                return normalized;
            }

            return normalized[..NormalizedCacheMessageMaxLength];
        }

        private static ChatbotAskResponse CloneCachedResponse(ChatbotAskResponse source)
        {
            return new ChatbotAskResponse
            {
                Role = source.Role,
                Answer = source.Answer,
                UsedFallback = source.UsedFallback,
                GeneratedAtUtc = DateTime.UtcNow
            };
        }

        private void CacheAnswer(string cacheKey, ChatbotAskResponse response)
        {
            if (response.UsedFallback)
            {
                return;
            }

            var cacheSeconds = ResolveAnswerCacheSeconds();
            if (cacheSeconds <= 0)
            {
                return;
            }

            _memoryCache.Set(cacheKey, CloneCachedResponse(response), TimeSpan.FromSeconds(cacheSeconds));
        }

        private bool IsProviderTemporarilyUnavailable(string provider)
        {
            return _memoryCache.TryGetValue(BuildProviderUnavailableCacheKey(provider), out _);
        }

        private void MarkProviderTemporarilyUnavailable(string provider)
        {
            var cooldown = ResolveProviderFailureCooldownSeconds();
            _memoryCache.Set(
                BuildProviderUnavailableCacheKey(provider),
                true,
                TimeSpan.FromSeconds(cooldown));
        }

        private int ResolveProviderFailureCooldownSeconds()
        {
            return _options.ProviderFailureCooldownSeconds <= 0 ? 60 : _options.ProviderFailureCooldownSeconds;
        }

        private static string BuildProviderUnavailableCacheKey(string provider)
        {
            return $"chatbot:provider:{provider.ToLowerInvariant()}:unavailable";
        }

        private static string NormalizeProvider(string? provider)
        {
            return string.Equals(provider, ProviderOllama, StringComparison.OrdinalIgnoreCase)
                ? ProviderOllama
                : ProviderOpenRouter;
        }

        private static string FormatUtcToLocal(DateTime dateTimeUtc)
        {
            return dateTimeUtc.ToLocalTime().ToString("dd MMM yyyy, hh:mm tt");
        }

        private static string NormalizeRole(string? role)
        {
            if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                return "Admin";
            }

            if (string.Equals(role, "Teacher", StringComparison.OrdinalIgnoreCase))
            {
                return "Teacher";
            }

            return "Student";
        }

        private static string? NormalizeHistoryRole(string? role)
        {
            if (string.Equals(role, "assistant", StringComparison.OrdinalIgnoreCase))
            {
                return "assistant";
            }

            if (string.Equals(role, "user", StringComparison.OrdinalIgnoreCase))
            {
                return "user";
            }

            return null;
        }

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            if (value.Length <= maxLength)
            {
                return value;
            }

            return value[..maxLength];
        }

        private sealed class RoleContext
        {
            public string Role { get; set; } = "Student";
            public List<string> DynamicFacts { get; set; } = new();
            public List<string> FeatureHints { get; set; } = new();
        }

        private sealed class ProviderResult
        {
            public bool Success { get; private set; }
            public string Answer { get; private set; } = string.Empty;
            public string ErrorReason { get; private set; } = "Provider request failed.";
            public bool ApplyCooldown { get; private set; }

            public static ProviderResult FromSuccess(string answer)
            {
                return new ProviderResult
                {
                    Success = true,
                    Answer = answer,
                    ApplyCooldown = false
                };
            }

            public static ProviderResult FromFailure(string reason, bool applyCooldown = true)
            {
                return new ProviderResult
                {
                    Success = false,
                    ErrorReason = string.IsNullOrWhiteSpace(reason) ? "Provider request failed." : reason,
                    ApplyCooldown = applyCooldown
                };
            }
        }

        private sealed class OpenRouterChatRequest
        {
            [JsonPropertyName("model")]
            public string Model { get; set; } = string.Empty;

            [JsonPropertyName("messages")]
            public List<OpenRouterMessage> Messages { get; set; } = new();

            [JsonPropertyName("temperature")]
            public double Temperature { get; set; }

            [JsonPropertyName("max_tokens")]
            public int MaxTokens { get; set; }

            [JsonPropertyName("stream")]
            public bool Stream { get; set; }
        }

        private sealed class OpenRouterChatResponse
        {
            [JsonPropertyName("choices")]
            public List<OpenRouterChoice>? Choices { get; set; }
        }

        private sealed class OpenRouterChoice
        {
            [JsonPropertyName("message")]
            public OpenRouterMessage? Message { get; set; }
        }

        private sealed class OpenRouterMessage
        {
            [JsonPropertyName("role")]
            public string Role { get; set; } = "user";

            [JsonPropertyName("content")]
            public string Content { get; set; } = string.Empty;
        }

        private sealed class OllamaChatRequest
        {
            [JsonPropertyName("model")]
            public string Model { get; set; } = string.Empty;

            [JsonPropertyName("stream")]
            public bool Stream { get; set; }

            [JsonPropertyName("messages")]
            public List<OllamaMessage> Messages { get; set; } = new();

            [JsonPropertyName("options")]
            public OllamaOptions Options { get; set; } = new();
        }

        private sealed class OllamaOptions
        {
            [JsonPropertyName("temperature")]
            public double Temperature { get; set; }

            [JsonPropertyName("num_predict")]
            public int NumPredict { get; set; }

            [JsonPropertyName("num_ctx")]
            public int NumContextWindow { get; set; }
        }

        private sealed class OllamaChatResponse
        {
            [JsonPropertyName("message")]
            public OllamaMessage? Message { get; set; }
        }

        private sealed class OllamaMessage
        {
            [JsonPropertyName("role")]
            public string Role { get; set; } = "user";

            [JsonPropertyName("content")]
            public string Content { get; set; } = string.Empty;
        }
    }
}
