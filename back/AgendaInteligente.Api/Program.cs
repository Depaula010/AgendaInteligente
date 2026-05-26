using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using Serilog;
using AgendaInteligente.Api.Common;
using AgendaInteligente.Api.Configuration;
using AgendaInteligente.Api.Data;
using AgendaInteligente.Api.Endpoints;
using AgendaInteligente.Api.HealthChecks;
using AgendaInteligente.Api.MultiTenancy;
using AgendaInteligente.Api.Repositories;
using AgendaInteligente.Api.Repositories.Interfaces;
using AgendaInteligente.Api.Services;
using AgendaInteligente.Api.Services.BackgroundServices;
using AgendaInteligente.Api.Services.Interfaces;
using AgendaInteligente.Api.Services.Redis;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;


var builder = WebApplication.CreateBuilder(args);

// ── Local overrides (gitignored, never committed) ──────────────────────────────
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

// ── Serilog (B31) ──────────────────────────────────────────────────────────────
builder.Host.UseSerilog((ctx, loggerConfig) =>
    loggerConfig.ReadFrom.Configuration(ctx.Configuration));

// ── Infrastructure ─────────────────────────────────────────────────────────────
builder.Services.AddHttpContextAccessor();

// ── Multi-tenancy ──────────────────────────────────────────────────────────────
// Scoped: cada requisição HTTP resolve o seu próprio TenantId
builder.Services.AddScoped<ITenantProvider, HttpContextTenantProvider>();

// ── Redis Distributed Cache + Streams ─────────────────────────────────────────
var redisConnection = builder.Configuration.GetConnectionString("RedisConnection");
if (!string.IsNullOrWhiteSpace(redisConnection))
{
    builder.Services.AddStackExchangeRedisCache(options => options.Configuration = redisConnection);

    // IConnectionMultiplexer compartilhado para Redis Streams (XADD / XREADGROUP)
    builder.Services.AddSingleton<IConnectionMultiplexer>(
        _ => ConnectionMultiplexer.Connect(redisConnection));

    builder.Services.Configure<RedisStreamOptions>(
        builder.Configuration.GetSection(RedisStreamOptions.SectionName));

    builder.Services.AddSingleton<IRedisStreamService, RedisStreamService>();
    builder.Services.AddHostedService<InboundStreamConsumerService>();
}
else
{
    // Fallback em memória para ambiente de testes/CI sem Redis
    builder.Services.AddDistributedMemoryCache();
    builder.Services.AddSingleton<IRedisStreamService, NullRedisStreamService>();
}

// ── Database (Entity Framework Core + PostgreSQL) ──────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' não encontrada. Verifique o appsettings.json.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString)
           .UseSnakeCaseNamingConvention()); // Converte PascalCase para snake_case no Postgres

// ── CORS ───────────────────────────────────────────────────────────────────────
var allowedOrigins = builder.Configuration
    .GetSection("AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173", "https://localhost:5173"];

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()));

// ── OpenAPI / Swagger ──────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "AgendaInteligente API",
        Version = "v1",
        Description = "API do SaaS de Agendamento Multi-Nicho — Barbearias, Clínicas, etc."
    });
});

// ── Repositories & Services ────────────────────────────────────────────────────
// Tenant
builder.Services.AddScoped<ITenantRepository, TenantRepository>();
builder.Services.AddScoped<TenantService>();

// Onboarding
builder.Services.AddScoped<IOnboardingRepository, OnboardingRepository>();
builder.Services.AddScoped<IOnboardingService, OnboardingService>();

// Professional
builder.Services.AddScoped<IProfessionalRepository, ProfessionalRepository>();
builder.Services.AddScoped<IProfessionalService, ProfessionalService>();

// ServiceCatalog (entidade Service → prefixo "Catalog" para evitar ambiguidade)
builder.Services.AddScoped<IServiceCatalogRepository, ServiceCatalogRepository>();
builder.Services.AddScoped<IServiceCatalogService, ServiceCatalogService>();

// Customer
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();

// Schedule
builder.Services.AddScoped<IScheduleRepository, ScheduleRepository>();
builder.Services.AddScoped<IScheduleService, ScheduleService>();

// TenantSettings
builder.Services.AddScoped<ITenantSettingsRepository, TenantSettingsRepository>();
builder.Services.AddScoped<ITenantSettingsService, TenantSettingsService>();

// Auth
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPasswordResetService, PasswordResetService>();

// Webhooks
builder.Services.AddScoped<IWebhookService, WebhookService>();
builder.Services.AddScoped<IBotIntentDispatcherService, BotIntentDispatcherService>();

// Web Push Notifications
builder.Services.Configure<VapidOptions>(builder.Configuration.GetSection(VapidOptions.SectionName));
builder.Services.AddScoped<IPushSubscriptionRepository, PushSubscriptionRepository>();
builder.Services.AddScoped<IWebPushService, WebPushService>();

// Conversation History (Redis)
builder.Services.AddScoped<IConversationHistoryService, ConversationHistoryService>();

// WhatsApp Send + Session (bot Node.js)
builder.Services.Configure<WhatsAppBotOptions>(builder.Configuration.GetSection(WhatsAppBotOptions.SectionName));
builder.Services.AddScoped<IWhatsAppSendService, WhatsAppSendService>();
builder.Services.AddScoped<IWhatsAppSessionService, WhatsAppSessionService>();

// Waitlist
builder.Services.AddScoped<IWaitlistRepository, WaitlistRepository>();
builder.Services.AddScoped<IWaitlistService, WaitlistService>();
builder.Services.AddScoped<IWhatsAppNotificationService, WhatsAppNotificationService>();

// Reminders (B27)
builder.Services.AddScoped<IReminderService, ReminderService>();
builder.Services.AddHostedService<ReminderBackgroundService>();

// Reengajamento automático (B34)
builder.Services.AddScoped<IReengagementService, ReengagementService>();
builder.Services.AddHostedService<ReengagementBackgroundService>();

// AI (Gemini)
builder.Services.Configure<GeminiOptions>(builder.Configuration.GetSection(GeminiOptions.SectionName));
builder.Services.AddHttpClient<IGeminiService, GeminiService>();
builder.Services.AddScoped<IAiOrchestratorService, AiOrchestratorService>();

// Google Calendar Sync
builder.Services.Configure<GoogleCalendarOptions>(builder.Configuration.GetSection(GoogleCalendarOptions.SectionName));
builder.Services.AddSingleton<ICalendarSyncQueue, CalendarSyncQueue>();
builder.Services.AddTransient<IGoogleCalendarApiService, GoogleCalendarApiService>();
builder.Services.AddHostedService<GoogleCalendarSyncBackgroundService>();


// ── Encryption (B38) ──────────────────────────────────────────────────────────
// Criptografa dados sensíveis em repouso (ex: GoogleCalendarRefreshToken).
// Se a chave não estiver configurada, usa NullEncryptionService (passthrough).
var encryptionKey = builder.Configuration.GetValue<string>("Encryption:Key");
if (!string.IsNullOrWhiteSpace(encryptionKey))
    builder.Services.AddSingleton<IEncryptionService>(new AesEncryptionService(encryptionKey));
else
    builder.Services.AddSingleton<IEncryptionService, NullEncryptionService>();

// ── Health Checks (B32) ───────────────────────────────────────────────────────
var healthChecks = builder.Services.AddHealthChecks()
    .AddNpgSql(
        connectionString,
        name:    "postgresql",
        tags:    ["db"],
        timeout: TimeSpan.FromSeconds(5))
    .AddCheck<BotHealthCheck>(
        name:    "whatsapp_bot",
        tags:    ["external"],
        timeout: TimeSpan.FromSeconds(6));

if (!string.IsNullOrWhiteSpace(redisConnection))
{
    healthChecks.AddRedis(
        redisConnection,
        name:    "redis",
        tags:    ["cache"],
        timeout: TimeSpan.FromSeconds(5));
}

builder.Services.AddTransient<BotHealthCheck>();

// ── Rate Limiting (B37 + B30) ─────────────────────────────────────────────────
// B37: Policy "webhook-per-tenant" — token bucket por tenantId no endpoint de webhook.
// B30: Policy global por IP — fixed window, proteção geral contra abuso/scraping.
builder.Services.AddRateLimiter(options =>
{
    // B37: webhook por tenant (200 tok, +10 a cada 3 s ≈ 200/min)
    options.AddPolicy("webhook-per-tenant", context =>
    {
        var tenantId = context.Request.RouteValues["tenantId"]?.ToString() ?? "unknown";
        return RateLimitPartition.GetTokenBucketLimiter(tenantId, _ => new TokenBucketRateLimiterOptions
        {
            TokenLimit            = 200,
            TokensPerPeriod       = 10,
            ReplenishmentPeriod   = TimeSpan.FromSeconds(3),
            QueueProcessingOrder  = QueueProcessingOrder.OldestFirst,
            QueueLimit            = 0,
            AutoReplenishment     = true,
        });
    });

    // B30: global por IP — fixed window 300 req/min (5 req/s de rajada máx.),
    // janela de 60 s renovada automaticamente.
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
    {
        var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit         = 300,
            Window              = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit          = 0,
        });
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (ctx, _) =>
    {
        ctx.HttpContext.Response.Headers.RetryAfter = "60";
        await ctx.HttpContext.Response.WriteAsJsonAsync(new { error = "Muitas requisições. Tente em instantes." });
    };
});

// ── Authentication & Authorization (JWT) ───────────────────────────────────────
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));

var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() 
                  ?? throw new InvalidOperationException("JwtSettings não configurado.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireOwnerRole", policy =>
        policy.RequireClaim("role", "Owner"));

    options.AddPolicy("RequireServiceManagementAccess", policy =>
        policy.RequireAssertion(ctx =>
            ctx.User.HasClaim("role", "Owner") ||
            (ctx.User.HasClaim("role", "Receptionist") &&
             ctx.User.HasClaim("can_manage_services", "true"))));
});

// ── Build ──────────────────────────────────────────────────────────────────────
var app = builder.Build();

// ── Middleware Pipeline ────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "AgendaInteligente v1"));
}

if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();
app.UseSerilogRequestLogging(opts =>
{
    opts.EnrichDiagnosticContext = (diagCtx, httpCtx) =>
    {
        diagCtx.Set("RemoteIpAddress", httpCtx.Connection.RemoteIpAddress?.ToString());
        diagCtx.Set("UserAgent",       httpCtx.Request.Headers.UserAgent.ToString());
    };
    // Silencia health probes para não poluir os logs
    opts.GetLevel = (ctx, _, _) =>
        ctx.Request.Path.StartsWithSegments("/health")
            ? Serilog.Events.LogEventLevel.Debug
            : Serilog.Events.LogEventLevel.Information;
});
app.UseCors();

// Habilita rebobinar o body em todas as requisições.
// Necessário para que o WebhookHmacFilter possa ler o body DEPOIS do model binding
// já o ter consumido (em Minimal API, binding ocorre antes dos endpoint filters).
app.Use(async (ctx, next) =>
{
    ctx.Request.EnableBuffering();
    await next(ctx);
});

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// ── Endpoints ──────────────────────────────────────────────────────────────────
// /health — liveness probe leve (sem dependências externas)
app.MapGet("/health", () => Results.Ok(new
{
    status    = "healthy",
    timestamp = DateTime.UtcNow,
    version   = "1.0.0"
}))
.WithName("HealthCheck")
.WithTags("System")
.AllowAnonymous();

// /health/detailed — readiness probe com checks de PostgreSQL, Redis e bot
app.MapHealthChecks("/health/detailed", new HealthCheckOptions
{
    AllowCachingResponses = false,
    ResponseWriter = async (ctx, report) =>
    {
        ctx.Response.ContentType = "application/json";
        var result = new
        {
            status    = report.Status.ToString().ToLower(),
            timestamp = DateTime.UtcNow,
            duration  = report.TotalDuration.TotalMilliseconds,
            checks    = report.Entries.ToDictionary(
                e => e.Key,
                e => new
                {
                    status      = e.Value.Status.ToString().ToLower(),
                    description = e.Value.Description,
                    duration    = e.Value.Duration.TotalMilliseconds,
                    error       = e.Value.Exception?.Message
                })
        };
        await ctx.Response.WriteAsync(JsonSerializer.Serialize(result,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }
})
.WithName("HealthCheckDetailed")
.WithTags("System")
.AllowAnonymous();

app.MapOnboardingEndpoints();
app.MapAuthEndpoints();
app.MapTenantEndpoints();

app.MapCustomerEndpoints();
app.MapProfessionalEndpoints();
app.MapServiceCatalogEndpoints();
app.MapScheduleEndpoints();
app.MapTenantSettingsEndpoints();
app.MapWebhookEndpoints();
app.MapWhatsAppEndpoints();
app.MapAiEndpoints();
app.MapPushEndpoints();

app.Run();

// Expõe a classe Program para testes de integração (xUnit WebApplicationFactory)
public partial class Program { }
