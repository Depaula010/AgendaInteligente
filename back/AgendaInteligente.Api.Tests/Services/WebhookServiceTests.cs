using AgendaInteligente.Api.Contracts.Requests.Webhook;
using AgendaInteligente.Api.Domain.Entities;
using AgendaInteligente.Api.Models.AI;
using AgendaInteligente.Api.Repositories.Interfaces;
using AgendaInteligente.Api.Services;
using AgendaInteligente.Api.Services.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AgendaInteligente.Api.Tests.Services;

public sealed class WebhookServiceTests
{
    private readonly Mock<IConversationHistoryService>  _historyMock;
    private readonly Mock<IAiOrchestratorService>       _aiMock;
    private readonly Mock<IBotIntentDispatcherService>  _dispatcherMock;
    private readonly Mock<IWhatsAppSendService>         _sendMock;
    private readonly Mock<ICustomerRepository>          _customerMock;
    private readonly WebhookService                     _service;

    public WebhookServiceTests()
    {
        _historyMock    = new Mock<IConversationHistoryService>();
        _aiMock         = new Mock<IAiOrchestratorService>();
        _dispatcherMock = new Mock<IBotIntentDispatcherService>();
        _sendMock       = new Mock<IWhatsAppSendService>();
        _customerMock   = new Mock<ICustomerRepository>();

        // Defaults: mensagem nova (não duplicada), histórico vazio
        _historyMock.Setup(h => h.IsMessageDuplicateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _historyMock.Setup(h => h.GetHistoryAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _historyMock.Setup(h => h.SaveHistoryAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<List<MessageHistory>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _aiMock.Setup(ai => ai.ProcessUserMessageAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<List<MessageHistory>>()))
            .ReturnsAsync(new GeminiIntentResponse { Intent = "general", ReplyMessage = "Posso ajudar com algo?" });

        // Dispatcher por padrão passa o reply da IA inalterado
        _dispatcherMock.Setup(d => d.DispatchAsync(It.IsAny<GeminiIntentResponse>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Posso ajudar com algo?");

        _sendMock.Setup(s => s.SendTextMessageAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Default B22: número desconhecido → cria Customer
        _customerMock.Setup(r => r.GetByPhoneAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);
        _customerMock.Setup(r => r.CreateAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer c, CancellationToken _) => c);

        _service = new WebhookService(
            _historyMock.Object,
            _aiMock.Object,
            _dispatcherMock.Object,
            _sendMock.Object,
            _customerMock.Object,
            new NullLogger<WebhookService>());
    }

    // ── Guard clauses ────────────────────────────────────────────────────────────

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

    // ── Loop completo ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ProcessWhatsAppMessageAsync_WithValidRequest_CallsAiOrchestrator()
    {
        await _service.ProcessWhatsAppMessageAsync(ValidRequest());

        _aiMock.Verify(ai => ai.ProcessUserMessageAsync(
            It.IsAny<Guid>(),
            "Olá, quero agendar",
            It.IsAny<List<MessageHistory>>()), Times.Once);
    }

    [Fact]
    public async Task ProcessWhatsAppMessageAsync_WithValidRequest_CallsDispatcherWithAiResponse()
    {
        GeminiIntentResponse? passedResponse = null;
        _dispatcherMock.Setup(d => d.DispatchAsync(It.IsAny<GeminiIntentResponse>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<GeminiIntentResponse, Guid, string, CancellationToken>((r, _, _, _) => passedResponse = r)
            .ReturnsAsync("Posso ajudar com algo?");

        await _service.ProcessWhatsAppMessageAsync(ValidRequest());

        Assert.NotNull(passedResponse);
        Assert.Equal("general", passedResponse!.Intent);
    }

    [Fact]
    public async Task ProcessWhatsAppMessageAsync_SendsDispatcherReply_NotDirectAiReply()
    {
        // Dispatcher retorna mensagem diferente da IA (ex: agendamento confirmado)
        _dispatcherMock.Setup(d => d.DispatchAsync(It.IsAny<GeminiIntentResponse>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Agendamento criado com sucesso!");

        await _service.ProcessWhatsAppMessageAsync(ValidRequest());

        _sendMock.Verify(s => s.SendTextMessageAsync(
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            "Agendamento criado com sucesso!",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessWhatsAppMessageAsync_WithValidRequest_SavesHistoryWithDispatcherReply()
    {
        var request = ValidRequest();
        _dispatcherMock.Setup(d => d.DispatchAsync(It.IsAny<GeminiIntentResponse>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Posso ajudar com algo?");

        List<MessageHistory>? savedHistory = null;
        _historyMock.Setup(h => h.SaveHistoryAsync(request.TenantId, request.SenderPhone, It.IsAny<List<MessageHistory>>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, string, List<MessageHistory>, CancellationToken>((_, _, history, _) => savedHistory = history)
            .Returns(Task.CompletedTask);

        await _service.ProcessWhatsAppMessageAsync(request);

        Assert.NotNull(savedHistory);
        Assert.Equal(2, savedHistory!.Count);
        Assert.Equal("user",  savedHistory[0].Role);
        Assert.Equal("model", savedHistory[1].Role);
        Assert.Equal(request.MessageText,      savedHistory[0].Content);
        Assert.Equal("Posso ajudar com algo?", savedHistory[1].Content);
    }

    [Fact]
    public async Task ProcessWhatsAppMessageAsync_WhenDuplicateMessage_SkipsProcessingEntirely()
    {
        _historyMock.Setup(h => h.IsMessageDuplicateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _service.ProcessWhatsAppMessageAsync(ValidRequest());

        _customerMock.Verify(r => r.CreateAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()), Times.Never);
        _aiMock.Verify(ai => ai.ProcessUserMessageAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<List<MessageHistory>>()), Times.Never);
        _dispatcherMock.Verify(d => d.DispatchAsync(It.IsAny<GeminiIntentResponse>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _sendMock.Verify(s => s.SendTextMessageAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessWhatsAppMessageAsync_WhenAiFails_DoesNotCallDispatcherOrSendReply()
    {
        _aiMock.Setup(ai => ai.ProcessUserMessageAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<List<MessageHistory>>()))
            .ThrowsAsync(new Exception("Gemini API error 429"));

        await _service.ProcessWhatsAppMessageAsync(ValidRequest());

        _dispatcherMock.Verify(d => d.DispatchAsync(It.IsAny<GeminiIntentResponse>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _sendMock.Verify(s => s.SendTextMessageAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessWhatsAppMessageAsync_WhenSendFails_DoesNotThrow()
    {
        _sendMock.Setup(s => s.SendTextMessageAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await _service.ProcessWhatsAppMessageAsync(ValidRequest());
    }

    [Fact]
    public async Task ProcessWhatsAppMessageAsync_PreviousHistoryIsPassedToAiOrchestrator()
    {
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

        await _service.ProcessWhatsAppMessageAsync(ValidRequest());

        Assert.NotNull(passedHistory);
        Assert.Equal(2, passedHistory!.Count);
        Assert.Equal("Oi", passedHistory[0].Content);
    }

    // ── B22 — Registro automático de Customer ────────────────────────────────────

    [Fact]
    public async Task ProcessWhatsAppMessageAsync_WhenNewPhone_CreatesCustomerWithCorrectData()
    {
        var request = ValidRequest();
        _customerMock.Setup(r => r.GetByPhoneAsync(request.SenderPhone, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        await _service.ProcessWhatsAppMessageAsync(request);

        _customerMock.Verify(r => r.CreateAsync(
            It.Is<Customer>(c =>
                c.PhoneNumber == request.SenderPhone &&
                c.Name        == request.SenderPhone &&
                c.TenantId    == request.TenantId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessWhatsAppMessageAsync_WhenExistingCustomer_DoesNotCallCreate()
    {
        var request = ValidRequest();
        _customerMock.Setup(r => r.GetByPhoneAsync(request.SenderPhone, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Customer { Name = request.SenderPhone, PhoneNumber = request.SenderPhone, TenantId = request.TenantId });

        await _service.ProcessWhatsAppMessageAsync(request);

        _customerMock.Verify(r => r.CreateAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessWhatsAppMessageAsync_WhenNewPhone_ContinuesToProcessMessageAfterCreation()
    {
        // Garante que a criação do customer não interrompe o fluxo normal
        _customerMock.Setup(r => r.GetByPhoneAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        await _service.ProcessWhatsAppMessageAsync(ValidRequest());

        _aiMock.Verify(ai => ai.ProcessUserMessageAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<List<MessageHistory>>()), Times.Once);
        _sendMock.Verify(s => s.SendTextMessageAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
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
