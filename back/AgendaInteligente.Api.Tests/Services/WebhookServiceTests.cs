using AgendaInteligente.Api.Contracts.Requests.Webhook;
using AgendaInteligente.Api.Models.AI;
using AgendaInteligente.Api.Services;
using AgendaInteligente.Api.Services.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AgendaInteligente.Api.Tests.Services;

public sealed class WebhookServiceTests
{
    private readonly Mock<IConversationHistoryService> _historyMock;
    private readonly Mock<IAiOrchestratorService> _aiMock;
    private readonly Mock<IWhatsAppSendService> _sendMock;
    private readonly WebhookService _service;

    public WebhookServiceTests()
    {
        _historyMock = new Mock<IConversationHistoryService>();
        _aiMock      = new Mock<IAiOrchestratorService>();
        _sendMock    = new Mock<IWhatsAppSendService>();

        // Defaults: mensagem nova (não duplicada), histórico vazio
        _historyMock.Setup(h => h.IsMessageDuplicateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _historyMock.Setup(h => h.GetHistoryAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _historyMock.Setup(h => h.SaveHistoryAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<List<MessageHistory>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _aiMock.Setup(ai => ai.ProcessUserMessageAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<List<MessageHistory>>()))
            .ReturnsAsync(new GeminiIntentResponse { Intent = "general", ReplyMessage = "Posso ajudar com algo?" });

        _sendMock.Setup(s => s.SendTextMessageAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _service = new WebhookService(
            _historyMock.Object,
            _aiMock.Object,
            _sendMock.Object,
            new NullLogger<WebhookService>());
    }

    // ── Guard clauses (comportamento já existente) ───────────────────────────────

    [Fact]
    public async Task ProcessWhatsAppMessageAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.ProcessWhatsAppMessageAsync(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task ProcessWhatsAppMessageAsync_WithInvalidSenderPhone_ThrowsArgumentException(string? invalidPhone)
    {
        var request = ValidRequest();
        request.SenderPhone = invalidPhone!;
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.ProcessWhatsAppMessageAsync(request));
        Assert.Contains("SenderPhone", ex.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task ProcessWhatsAppMessageAsync_WithInvalidMessageText_ThrowsArgumentException(string? invalidText)
    {
        var request = ValidRequest();
        request.MessageText = invalidText!;
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.ProcessWhatsAppMessageAsync(request));
        Assert.Contains("MessageText", ex.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task ProcessWhatsAppMessageAsync_WithInvalidMessageId_ThrowsArgumentException(string? invalidId)
    {
        var request = ValidRequest();
        request.MessageId = invalidId!;
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.ProcessWhatsAppMessageAsync(request));
        Assert.Contains("MessageId", ex.ParamName);
    }

    [Fact]
    public async Task ProcessWhatsAppMessageAsync_WithEmptyTenantId_ThrowsArgumentException()
    {
        var request = ValidRequest();
        request.TenantId = Guid.Empty;
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.ProcessWhatsAppMessageAsync(request));
        Assert.Contains("TenantId", ex.ParamName);
    }

    // ── Loop completo — novos cenários ───────────────────────────────────────────

    [Fact]
    public async Task ProcessWhatsAppMessageAsync_WithValidRequest_CallsAiOrchestrator()
    {
        // Act
        await _service.ProcessWhatsAppMessageAsync(ValidRequest());

        // Assert
        _aiMock.Verify(ai => ai.ProcessUserMessageAsync(
            It.IsAny<Guid>(),
            "Olá, quero agendar",
            It.IsAny<List<MessageHistory>>()), Times.Once);
    }

    [Fact]
    public async Task ProcessWhatsAppMessageAsync_WithValidRequest_SavesHistoryWithUserAndModelMessages()
    {
        // Arrange
        var request = ValidRequest();
        List<MessageHistory>? savedHistory = null;
        _historyMock.Setup(h => h.SaveHistoryAsync(request.TenantId, request.SenderPhone, It.IsAny<List<MessageHistory>>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, string, List<MessageHistory>, CancellationToken>((_, _, history, _) => savedHistory = history)
            .Returns(Task.CompletedTask);

        // Act
        await _service.ProcessWhatsAppMessageAsync(request);

        // Assert
        Assert.NotNull(savedHistory);
        Assert.Equal(2, savedHistory!.Count);
        Assert.Equal("user",  savedHistory[0].Role);
        Assert.Equal("model", savedHistory[1].Role);
        Assert.Equal(request.MessageText,         savedHistory[0].Content);
        Assert.Equal("Posso ajudar com algo?",    savedHistory[1].Content);
    }

    [Fact]
    public async Task ProcessWhatsAppMessageAsync_WithValidRequest_SendsAiReplyToClient()
    {
        // Act
        await _service.ProcessWhatsAppMessageAsync(ValidRequest());

        // Assert
        _sendMock.Verify(s => s.SendTextMessageAsync(
            It.IsAny<Guid>(),
            "5511999999999",
            "Posso ajudar com algo?",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessWhatsAppMessageAsync_WhenDuplicateMessage_SkipsProcessingEntirely()
    {
        // Arrange
        _historyMock.Setup(h => h.IsMessageDuplicateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _service.ProcessWhatsAppMessageAsync(ValidRequest());

        // Assert — IA e envio NÃO devem ser chamados
        _aiMock.Verify(ai => ai.ProcessUserMessageAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<List<MessageHistory>>()), Times.Never);
        _sendMock.Verify(s => s.SendTextMessageAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessWhatsAppMessageAsync_WhenAiFails_DoesNotSendReply()
    {
        // Arrange — Gemini falhou (ex: API Key inválida)
        _aiMock.Setup(ai => ai.ProcessUserMessageAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<List<MessageHistory>>()))
            .ThrowsAsync(new Exception("Gemini API error 429"));

        // Act — não deve propagar exceção
        await _service.ProcessWhatsAppMessageAsync(ValidRequest());

        // Assert
        _sendMock.Verify(s => s.SendTextMessageAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessWhatsAppMessageAsync_WhenSendFails_DoesNotThrow()
    {
        // Arrange — bot Node.js indisponível
        _sendMock.Setup(s => s.SendTextMessageAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act & Assert — resiliência: não deve lançar exceção
        await _service.ProcessWhatsAppMessageAsync(ValidRequest());
    }

    [Fact]
    public async Task ProcessWhatsAppMessageAsync_PreviousHistoryIsPassedToAiOrchestrator()
    {
        // Arrange — simula conversa com histórico existente
        var existingHistory = new List<MessageHistory>
        {
            new() { Role = "user",  Content = "Oi" },
            new() { Role = "model", Content = "Olá! Em que posso ajudar?" }
        };
        _historyMock.Setup(h => h.GetHistoryAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingHistory);

        List<MessageHistory>? passedHistory = null;
        _aiMock.Setup(ai => ai.ProcessUserMessageAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<List<MessageHistory>>()))
            .Callback<Guid, string, List<MessageHistory>>((_, _, history) => passedHistory = history)
            .ReturnsAsync(new GeminiIntentResponse { Intent = "general", ReplyMessage = "Para quando?" });

        // Act
        await _service.ProcessWhatsAppMessageAsync(ValidRequest());

        // Assert — histórico carregado do Redis deve chegar à IA
        Assert.NotNull(passedHistory);
        Assert.Equal(2, passedHistory!.Count);
        Assert.Equal("Oi", passedHistory[0].Content);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────

    private static WebhookMessageRequest ValidRequest() => new()
    {
        TenantId    = Guid.NewGuid(),
        SenderPhone = "5511999999999",
        MessageText = "Olá, quero agendar",
        MessageId   = $"msg-{Guid.NewGuid()}",
        Timestamp   = DateTime.UtcNow
    };
}
