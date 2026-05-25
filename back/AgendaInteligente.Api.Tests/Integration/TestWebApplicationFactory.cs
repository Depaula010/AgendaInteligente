using System.Text;
using AgendaInteligente.Api.Contracts.Models;
using AgendaInteligente.Api.Data;
using AgendaInteligente.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Moq;

namespace AgendaInteligente.Api.Tests.Integration;

public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string TestApiKey  = "test-api-key-integration";
    public const string JwtSecret   = "TestIntegrationSecretKeyMustBe32Chars!!1";
    public const string JwtIssuer   = "AgendaInteligenteApi";
    public const string JwtAudience = "AgendaInteligentePWA";

    // Root compartilhado: garante que todos os AppDbContext scopes usem
    // o mesmo armazenamento InMemory, independente do fingerprint de opções do EF Core.
    private readonly InMemoryDatabaseRoot _dbRoot = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=fake;Database=test;Username=test;Password=test",
                ["ConnectionStrings:RedisConnection"]   = "",
                ["WebhookSettings:ApiKey"]              = TestApiKey,
                ["JwtSettings:Secret"]                  = JwtSecret,
                ["JwtSettings:Issuer"]                  = JwtIssuer,
                ["JwtSettings:Audience"]                = JwtAudience,
                ["JwtSettings:ExpiryMinutes"]           = "60",
                ["WhatsAppBot:WebhookSignatureKey"]      = "",
                ["Encryption:Key"]                      = "",
            });
        });

        builder.ConfigureServices(services =>
        {
            // ── EF Core — substitui Npgsql por InMemory ──────────────────────────
            // AddDbContext usa TryAdd para o tipo de contexto (AppDbContext), então
            // a chamada de re-registro é no-op se a original ainda existir.
            // Removemos TUDO relacionado a AppDbContext antes de re-adicionar.
            services.RemoveAll(typeof(AppDbContext));
            services.RemoveAll(typeof(DbContextOptions<AppDbContext>));

            var configType = typeof(IDbContextOptionsConfiguration<AppDbContext>);
            var toRemove   = services.Where(d => d.ServiceType == configType).ToList();
            foreach (var d in toRemove) services.Remove(d);

            // InMemoryDatabaseRoot garante que todos os contextos (scopes diferentes)
            // compartilhem o mesmo banco de dados em memória — sem root, o EF pode
            // criar stores separados quando o fingerprint de opções diverge entre scopes.
            var dbRoot = _dbRoot;
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase("TestDb", dbRoot));

            // ── JWT — sobrescreve a chave de validação em runtime ─────────────────
            // Program.cs lê o segredo via builder.Configuration antes de ConfigureWebHost
            // completar; PostConfigure roda depois, garantindo a chave de teste.
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, opts =>
            {
                opts.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer           = true,
                    ValidateAudience         = true,
                    ValidateLifetime         = true,
                    ValidateIssuerSigningKey  = true,
                    ValidIssuer              = JwtIssuer,
                    ValidAudience            = JwtAudience,
                    IssuerSigningKey         = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(JwtSecret)),
                    ClockSkew = TimeSpan.Zero,
                };
            });

            // ── WhatsApp stub — evita chamadas HTTP ao bot ────────────────────────
            services.RemoveAll<IWhatsAppSendService>();
            services.AddScoped<IWhatsAppSendService>(_ =>
            {
                var mock = new Mock<IWhatsAppSendService>();
                mock.Setup(s => s.SendTextMessageAsync(
                        It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true);
                mock.Setup(s => s.SendInteractiveListAsync(
                        It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<InteractiveListPayload>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true);
                return mock.Object;
            });
        });
    }
}
