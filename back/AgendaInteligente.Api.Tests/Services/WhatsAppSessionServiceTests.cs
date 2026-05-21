using AgendaInteligente.Api.Configuration;
using AgendaInteligente.Api.Domain.Entities;
using AgendaInteligente.Api.MultiTenancy;
using AgendaInteligente.Api.Repositories.Interfaces;
using AgendaInteligente.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace AgendaInteligente.Api.Tests.Services;

public sealed class WhatsAppSessionServiceTests
{
    private readonly Mock<IHttpClientFactory>        _httpClientFactoryMock;
    private readonly Mock<ITenantSettingsRepository> _settingsRepoMock;
    private readonly Mock<ITenantProvider>           _tenantProviderMock;
    private readonly WhatsAppBotOptions              _options;

    private static readonly Guid ValidTenantId  = Guid.NewGuid();
    private static readonly Guid ValidSessionId = Guid.NewGuid();

    public WhatsAppSessionServiceTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _settingsRepoMock      = new Mock<ITenantSettingsRepository>();
        _tenantProviderMock    = new Mock<ITenantProvider>();

        _options = new WhatsAppBotOptions
        {
            BotUrl               = "http://localhost:3000",
            BotApiKey            = "test-api-key",
            WebhookBackendUrl    = "https://api.test.com",
            WebhookSignatureKey  = "test-sig-key"
        };

        _tenantProviderMock.Setup(t => t.CurrentTenantId).Returns(ValidTenantId);
    }

    private WhatsAppSessionService CreateService(WhatsAppBotOptions? options = null)
    {
        var opts = Options.Create(options ?? _options);
        return new WhatsAppSessionService(
            _httpClientFactoryMock.Object,
            opts,
            _settingsRepoMock.Object,
            _tenantProviderMock.Object,
            new NullLogger<WhatsAppSessionService>());
    }

    // ── CreateAndConnectAsync — guard clauses ────────────────────────────────────

    [Fact]
    public async Task CreateAndConnectAsync_WhenBotUrlNotConfigured_ReturnsFail()
    {
        var service = CreateService(new WhatsAppBotOptions { BotUrl = string.Empty });

        var result = await service.CreateAndConnectAsync();

        Assert.False(result.IsSuccess);
        Assert.Contains("BotUrl", result.Error);
    }

    [Fact]
    public async Task CreateAndConnectAsync_WhenTenantIdNotResolved_ReturnsFail()
    {
        _tenantProviderMock.Setup(t => t.CurrentTenantId).Returns((Guid?)null);
        var service = CreateService();

        var result = await service.CreateAndConnectAsync();

        Assert.False(result.IsSuccess);
        Assert.Contains("TenantId", result.Error);
    }

    [Fact]
    public async Task CreateAndConnectAsync_WhenSettingsNotFound_ReturnsFail()
    {
        _settingsRepoMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantSettings?)null);
        var service = CreateService();

        var result = await service.CreateAndConnectAsync();

        Assert.False(result.IsSuccess);
        Assert.Contains("Configurações", result.Error);
    }

    // ── GetStatusAsync — guard clauses ───────────────────────────────────────────

    [Fact]
    public async Task GetStatusAsync_WhenBotUrlNotConfigured_ReturnsFail()
    {
        var service = CreateService(new WhatsAppBotOptions { BotUrl = string.Empty });

        var result = await service.GetStatusAsync();

        Assert.False(result.IsSuccess);
        Assert.Contains("BotUrl", result.Error);
    }

    [Fact]
    public async Task GetStatusAsync_WhenSettingsNotFound_ReturnsFail()
    {
        _settingsRepoMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantSettings?)null);
        var service = CreateService();

        var result = await service.GetStatusAsync();

        Assert.False(result.IsSuccess);
        Assert.Contains("Configurações", result.Error);
    }

    [Fact]
    public async Task GetStatusAsync_WhenNoBotSessionId_ReturnsNotConfiguredStatus()
    {
        _settingsRepoMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TenantSettings { TenantId = ValidTenantId, BotSessionId = null });
        var service = CreateService();

        var result = await service.GetStatusAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal("not_configured", result.Value.Status);
        Assert.False(result.Value.IsConnected);
        Assert.Null(result.Value.QrCode);
    }
}
