using AgendaInteligente.Api.Common;
using AgendaInteligente.Api.Configuration;
using AgendaInteligente.Api.Data;
using AgendaInteligente.Api.Endpoints;
using AgendaInteligente.Api.MultiTenancy;
using AgendaInteligente.Api.Repositories;
using AgendaInteligente.Api.Repositories.Interfaces;
using AgendaInteligente.Api.Services;
using AgendaInteligente.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;


var builder = WebApplication.CreateBuilder(args);

// ── Infrastructure ─────────────────────────────────────────────────────────────
builder.Services.AddHttpContextAccessor();

// ── Multi-tenancy ──────────────────────────────────────────────────────────────
// Scoped: cada requisição HTTP resolve o seu próprio TenantId
builder.Services.AddScoped<ITenantProvider, HttpContextTenantProvider>();

// ── Redis Distributed Cache ────────────────────────────────────────────────────
var redisConnection = builder.Configuration.GetConnectionString("RedisConnection");
if (!string.IsNullOrWhiteSpace(redisConnection))
{
    builder.Services.AddStackExchangeRedisCache(options => options.Configuration = redisConnection);
}
else
{
    // Fallback em memória para ambiente de testes/CI sem Redis
    builder.Services.AddDistributedMemoryCache();
}

// ── Database (Entity Framework Core + PostgreSQL) ──────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' não encontrada. Verifique o appsettings.json.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString)
           .UseSnakeCaseNamingConvention()); // Converte PascalCase para snake_case no Postgres

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

// Webhooks
builder.Services.AddScoped<IWebhookService, WebhookService>();
builder.Services.AddScoped<IBotIntentDispatcherService, BotIntentDispatcherService>();

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

// AI (Gemini)
builder.Services.Configure<GeminiOptions>(builder.Configuration.GetSection(GeminiOptions.SectionName));
builder.Services.AddHttpClient<IGeminiService, GeminiService>();
builder.Services.AddScoped<IAiOrchestratorService, AiOrchestratorService>();

// Google Calendar Sync
builder.Services.Configure<GoogleCalendarOptions>(builder.Configuration.GetSection(GoogleCalendarOptions.SectionName));
builder.Services.AddSingleton<ICalendarSyncQueue, CalendarSyncQueue>();
builder.Services.AddTransient<IGoogleCalendarApiService, GoogleCalendarApiService>();
builder.Services.AddHostedService<GoogleCalendarSyncBackgroundService>();


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
});

// ── Build ──────────────────────────────────────────────────────────────────────
var app = builder.Build();

// ── Middleware Pipeline ────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "AgendaInteligente v1"));
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// ── Endpoints ──────────────────────────────────────────────────────────────────
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    version = "1.0.0"
}))
.WithName("HealthCheck")
.WithTags("System")
.AllowAnonymous();

app.MapOnboardingEndpoints();
app.MapAuthEndpoints();
app.MapTenantEndpoints();

app.MapProfessionalEndpoints();
app.MapServiceCatalogEndpoints();
app.MapScheduleEndpoints();
app.MapTenantSettingsEndpoints();
app.MapWebhookEndpoints();
app.MapWhatsAppEndpoints();
app.MapAiEndpoints();

app.Run();

// Expõe a classe Program para testes de integração (xUnit WebApplicationFactory)
public partial class Program { }
