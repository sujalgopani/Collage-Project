using ExamNest.Data;
using ExamNest.Services;
using ExamNest.Services.Chatbot;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
const long MaxUploadSizeBytes = 20L * 1024 * 1024;

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => {
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });
});

// DbContext
builder.Services.AddDbContextPool<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));



// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:Key"]!)),
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };
    });

builder.Services.AddCors(opt => {
    opt.AddPolicy("AngularCord", policy =>
    {
        policy.WithOrigins(
                "http://localhost:4200",
                "https://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("TeacherOnly", policy => policy.RequireRole("Teacher"));
    options.AddPolicy("StudentOnly", policy => policy.RequireRole("Student"));
});
builder.Services.AddMemoryCache();
builder.Services.Configure<ChatbotOptions>(builder.Configuration.GetSection("Chatbot"));
builder.Services.AddHttpClient("OpenRouterChat", (serviceProvider, client) =>
{
    var chatbotOptions = serviceProvider.GetRequiredService<IOptions<ChatbotOptions>>().Value;
    var baseUrl = string.IsNullOrWhiteSpace(chatbotOptions.OpenRouterBaseUrl)
        ? "https://openrouter.ai/api/v1"
        : chatbotOptions.OpenRouterBaseUrl.Trim();

    if (!baseUrl.EndsWith('/'))
    {
        baseUrl += "/";
    }

    client.BaseAddress = new Uri(baseUrl);

    var timeoutSeconds = chatbotOptions.OpenRouterTimeoutSeconds <= 0 ? 20 : chatbotOptions.OpenRouterTimeoutSeconds;
    client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
});
builder.Services.AddHttpClient("OllamaChat", (serviceProvider, client) =>
{
    var chatbotOptions = serviceProvider.GetRequiredService<IOptions<ChatbotOptions>>().Value;
    var baseUrl = string.IsNullOrWhiteSpace(chatbotOptions.OllamaBaseUrl)
        ? "http://localhost:11434"
        : chatbotOptions.OllamaBaseUrl.Trim();

    if (!baseUrl.EndsWith('/'))
    {
        baseUrl += "/";
    }

    client.BaseAddress = new Uri(baseUrl);

    var timeoutSeconds = chatbotOptions.TimeoutSeconds <= 0 ? 90 : chatbotOptions.TimeoutSeconds;
    client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
});

builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = MaxUploadSizeBytes;
});

builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = MaxUploadSizeBytes;
});
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = MaxUploadSizeBytes;
});

// Register Business Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<AppActivityEmailService>();
builder.Services.AddSingleton<IGoogleAuthConfiguration, GoogleAuthConfiguration>();
builder.Services.AddScoped<IGoogleTokenValidator, GoogleTokenValidator>();
builder.Services.AddScoped<AdminServices>();
builder.Services.AddScoped<Student>();
builder.Services.AddScoped<ExamService>();
builder.Services.AddScoped<IAppChatbotService, AppChatbotService>();




var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseStartup");
    var db = services.GetRequiredService<AppDbContext>();

    try
    {
        await db.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Database migration step failed. Continuing with compatibility bootstrap.");
    }

    try
    {
        // Keep legacy databases compatible when migration history is out of sync.
        await db.Database.ExecuteSqlRawAsync(@"
IF COL_LENGTH('users', 'profile_image_url') IS NULL
BEGIN
    ALTER TABLE [users] ADD [profile_image_url] NVARCHAR(500) NULL;
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_users_role_id_is_active'
      AND object_id = OBJECT_ID(N'[users]')
)
BEGIN
    CREATE INDEX [IX_users_role_id_is_active] ON [users]([role_id], [is_active]);
END;

IF COL_LENGTH('Exams', 'IsResultPublished') IS NULL
BEGIN
    ALTER TABLE [Exams] ADD [IsResultPublished] BIT NOT NULL CONSTRAINT [DF_Exams_IsResultPublished] DEFAULT(0);
END;

IF COL_LENGTH('Exams', 'ResultPublishedAt') IS NULL
BEGIN
    ALTER TABLE [Exams] ADD [ResultPublishedAt] DATETIME2 NULL;
END;

IF OBJECT_ID(N'[Suggestions]', N'U') IS NULL
BEGIN
    CREATE TABLE [Suggestions] (
        [Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Suggestions] PRIMARY KEY,
        [StudentId] INT NOT NULL,
        [TeacherId] INT NOT NULL,
        [Title] NVARCHAR(MAX) NULL,
        [Message] NVARCHAR(MAX) NULL,
        [Reply] NVARCHAR(MAX) NULL,
        [Status] NVARCHAR(MAX) NULL,
        [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_Suggestions_CreatedAt] DEFAULT (SYSUTCDATETIME())
    );
END;

IF COL_LENGTH('Suggestions', 'StudentId') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1
       FROM sys.indexes
       WHERE name = 'IX_Suggestions_StudentId'
         AND object_id = OBJECT_ID(N'[Suggestions]')
   )
BEGIN
    CREATE INDEX [IX_Suggestions_StudentId] ON [Suggestions]([StudentId]);
END;

IF COL_LENGTH('Suggestions', 'TeacherId') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1
       FROM sys.indexes
       WHERE name = 'IX_Suggestions_TeacherId'
         AND object_id = OBJECT_ID(N'[Suggestions]')
   )
BEGIN
    CREATE INDEX [IX_Suggestions_TeacherId] ON [Suggestions]([TeacherId]);
END;

IF OBJECT_ID(N'[LiveClassSchedules]', N'U') IS NULL
BEGIN
    CREATE TABLE [LiveClassSchedules] (
        [LiveClassScheduleId] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_LiveClassSchedules] PRIMARY KEY,
        [CourseId] INT NOT NULL,
        [TeacherId] INT NOT NULL,
        [ScheduledByAdminId] INT NOT NULL,
        [Title] NVARCHAR(150) NOT NULL,
        [Agenda] NVARCHAR(1000) NULL,
        [MeetingLink] NVARCHAR(1000) NOT NULL,
        [StartAt] DATETIME2 NOT NULL,
        [EndAt] DATETIME2 NOT NULL,
        [MaterialTitle] NVARCHAR(200) NULL,
        [MaterialDescription] NVARCHAR(1000) NULL,
        [MaterialLink] NVARCHAR(1000) NULL,
        [MaterialFilePath] NVARCHAR(500) NULL,
        [IsCancelled] BIT NOT NULL CONSTRAINT [DF_LiveClassSchedules_IsCancelled] DEFAULT (0),
        [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_LiveClassSchedules_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] DATETIME2 NULL
    );
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_LiveClassSchedules_CourseId_StartAt'
      AND object_id = OBJECT_ID(N'[LiveClassSchedules]')
)
BEGIN
    CREATE INDEX [IX_LiveClassSchedules_CourseId_StartAt]
    ON [LiveClassSchedules]([CourseId], [StartAt]);
END;

IF OBJECT_ID(N'[LiveClassSchedules]', N'U') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1
       FROM sys.foreign_keys
       WHERE name = 'FK_LiveClassSchedules_Courses_CourseId'
   )
BEGIN
    ALTER TABLE [LiveClassSchedules]
    ADD CONSTRAINT [FK_LiveClassSchedules_Courses_CourseId]
        FOREIGN KEY ([CourseId]) REFERENCES [Courses]([CourseId]) ON DELETE CASCADE;
END;

IF OBJECT_ID(N'[LiveClassSchedules]', N'U') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1
       FROM sys.foreign_keys
       WHERE name = 'FK_LiveClassSchedules_users_TeacherId'
   )
BEGIN
    ALTER TABLE [LiveClassSchedules]
    ADD CONSTRAINT [FK_LiveClassSchedules_users_TeacherId]
        FOREIGN KEY ([TeacherId]) REFERENCES [users]([user_id]);
END;

IF OBJECT_ID(N'[LiveClassSchedules]', N'U') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1
       FROM sys.foreign_keys
       WHERE name = 'FK_LiveClassSchedules_users_ScheduledByAdminId'
   )
BEGIN
    ALTER TABLE [LiveClassSchedules]
    ADD CONSTRAINT [FK_LiveClassSchedules_users_ScheduledByAdminId]
        FOREIGN KEY ([ScheduledByAdminId]) REFERENCES [users]([user_id]);
END;");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database compatibility bootstrap failed during startup.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();
app.UseResponseCompression();

app.UseCors("AngularCord");

app.UseAuthentication();
app.UseAuthorization();

app.UseStaticFiles();

app.MapControllers();
app.Run();
