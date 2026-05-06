using AgendaInteligente.Api.Contracts.Requests.Webhook;
using AgendaInteligente.Api.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace AgendaInteligente.Api.Tests.Services;

public class WebhookServiceTests
{
    private readonly Mock<ILogger<WebhookService>> _loggerMock;
    private readonly WebhookService _service;

    public WebhookServiceTests()
    {
        _loggerMock = new Mock<ILogger<WebhookService>>();
        _service = new WebhookService(_loggerMock.Object);
    }

    [Fact]
    public async Task ProcessWhatsAppMessageAsync_WithValidRequest_ReturnsCompletedTask()
    {
        // Arrange
        var request = new WebhookMessageRequest
        {
            TenantId = Guid.NewGuid(),
            SenderPhone = "5511999999999",
            MessageText = "Olá, quero agendar",
            MessageId = "msg-123",
            Timestamp = DateTime.UtcNow
        };

        // Act
        await _service.ProcessWhatsAppMessageAsync(request);

        // Assert
        // Se não lançou exceção, o teste passa (happy-path).
        Assert.True(true);
    }

    [Fact]
    public async Task ProcessWhatsAppMessageAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.ProcessWhatsAppMessageAsync(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task ProcessWhatsAppMessageAsync_WithInvalidSenderPhone_ThrowsArgumentException(string? invalidPhone)
    {
        // Arrange
        var request = new WebhookMessageRequest
        {
            TenantId = Guid.NewGuid(),
            SenderPhone = invalidPhone!,
            MessageText = "Olá",
            MessageId = "msg-123"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.ProcessWhatsAppMessageAsync(request));
        Assert.Contains("SenderPhone", ex.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task ProcessWhatsAppMessageAsync_WithInvalidMessageText_ThrowsArgumentException(string? invalidText)
    {
        // Arrange
        var request = new WebhookMessageRequest
        {
            TenantId = Guid.NewGuid(),
            SenderPhone = "5511999999999",
            MessageText = invalidText!,
            MessageId = "msg-123"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.ProcessWhatsAppMessageAsync(request));
        Assert.Contains("MessageText", ex.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task ProcessWhatsAppMessageAsync_WithInvalidMessageId_ThrowsArgumentException(string? invalidId)
    {
        // Arrange
        var request = new WebhookMessageRequest
        {
            TenantId = Guid.NewGuid(),
            SenderPhone = "5511999999999",
            MessageText = "Olá",
            MessageId = invalidId!
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.ProcessWhatsAppMessageAsync(request));
        Assert.Contains("MessageId", ex.ParamName);
    }

    [Fact]
    public async Task ProcessWhatsAppMessageAsync_WithEmptyTenantId_ThrowsArgumentException()
    {
        // Arrange
        var request = new WebhookMessageRequest
        {
            TenantId = Guid.Empty,
            SenderPhone = "5511999999999",
            MessageText = "Olá",
            MessageId = "msg-123"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.ProcessWhatsAppMessageAsync(request));
        Assert.Contains("TenantId", ex.ParamName);
    }
}
