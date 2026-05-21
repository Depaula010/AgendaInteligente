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
    private readonly Mock<ICustomerRepository>          _customerMock;
    private readonly WebhookService                     _service;

    private static readonly Guid ValidTenantId = Guid.NewGuid();

    public WebhookServiceTests()
    {
        _historyMock    = new Mock<IConversationHistoryService>();
        _aiMock         = new Mock<IAiOrchestratorService>();
        _dispatcherMock = new Mock<IBotIntentDispatcherService>();
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

        _dispatcherMock.Setup(d => d.DispatchAsync(It.IsAny<GeminiIntentResponse>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Posso ajudar com algo?");

        // Default B22: número desconhecido → cria Customer
        _customerMock.Setup(r => r.GetByPhoneAndTenantAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);
        _customerMock.Setup(r => r.CreateAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer c, CancellationToken _) => c);

        _service = new WebhookService(
            _historyMock.Object,
            _aiMock.Object,
            _dispatcherMock.Object,
            _customerMock.Object,
            new NullLogger<WebhookService>());
    }

    // ── Guard clauses ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ProcessWhatsAppMessageAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.ProcessWhatsAppMessageAsync(ValidTenantId, null!));
    }

    [Fact]
    public async Task ProcessWhatsAppMessageAsync_WithEmptyTenantId_ThrowsArgumentException()
    {
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.ProcessWhatsAppMessageAsync(Guid.Empty, ValidBotRequest()));
        Assert.Contains("tenantId", ex.ParamName, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task ProcessWhatsAppMessageAsync_WithInvalidNumeroRemetente_ThrowsArgumentException(string? invalid)
    {
        var request = ValidBotRequest();
        request.NumeroRemetente = invalid!;
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.ProcessWhatsAppMessageAsync(ValidTenantId, request));
        Assert.Contains("NumeroRemetente", ex.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task ProcessWhatsAppMessageAsync_WithInvalidTexto_ThrowsArgumentException(string? invalid)
    {
        var request = ValidBotRequest();
        request.Texto = invalid!;
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.ProcessWhatsAppMessageAsync(ValidTenantId, request));
        Assert.Contains("Texto", ex.ParamName);
    }

    // ── Retorno de valor ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ProcessWhatsAppMessageAsync_WithValidRequest_ReturnsDispatcherReply()
    {
        _dispatcherMock.Setup(d => d.DispatchAsync(It.IsAny<GeminiIntentResponse>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Agendamento criado com sucesso!");

        var reply = await _service.ProcessWhatsAppMessageAsync(ValidTenantId, ValidBotRequest());

        Assert.Equal("Agendamento criado com sucesso!", reply);
    }

    [Fact]
    public async Task ProcessWhatsAppMessageAsync_WhenDuplicateMessage_ReturnsEmptyString()
    {
        _historyMock.Setup(h => h.IsMessageDuplicateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var reply = await _service.ProcessWhatsAppMessageAsync(ValidTenantId, ValidBotRequest());

        Assert.Equal(string.Empty, reply);
    }

    // ── Loop completo ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ProcessWhatsAppMessageAsync_WithValidRequest_CallsAiOrchestrator()
    {
        var request = ValidBotRequest();

        await _service.ProcessWhatsAppMessageAsync(ValidTenantId, request);

        _aiMock.Verify(ai => ai.ProcessUserMessageAsync(
            ValidTenantId,
            request.Texto,
            It.IsAny<List<MessageHistory>>()), Times.Once);
    }

    [Fact]
    public async Task ProcessWhatsAppMessageAsync_WithValidRequest_CallsDispatcherWithAiResponse()
    {
        GeminiIntentResponse? passedResponse = null;
        _dispatcherMock.Setup(d => d.DispatchAsync(It.IsAny<GeminiIntentResponse>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<GeminiIntentResponse, Guid, string, CancellationToken>((r, _, _, _) => passedResponse = r)
            .ReturnsAsync("Posso ajudar com algo?");

        await _service.ProcessWhatsAppMessageAsync(ValidTenantId, ValidBotRequest());

        Assert.NotNull(passedResponse);
        Assert.Equal("general", passedResponse!.Intent);
    }

    [Fact]
    public async Task ProcessWhatsAppMessageAsync_WithValidRequest_SavesHistoryWithDispatcherReply()
    {
        var request = ValidBotRequest();
        _dispatcherMock.Setup(d => d.DispatchAsync(It.IsAny<GeminiIntentResponse>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Posso ajudar com algo?");

        List<MessageHistory>? savedHistory = null;
        _historyMock.Setup(h => h.SaveHistoryAsync(ValidTenantId, request.NumeroRemetente, It.IsAny<List<MessageHistory>>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, string, List<MessageHistory>, CancellationToken>((_, _, history, _) => savedHistory = history)
            .Returns(Task.CompletedTask);

        await _service.ProcessWhatsAppMessageAsync(ValidTenantId, request);

        Assert.NotNull(savedHistory);
        Assert.Equal(2, savedHistory!.Count);
        Assert.Equal("user",  savedHistory[0].Role);
        Assert.Equal("model", savedHistory[1].Role);
        Assert.Equal(request.Texto,            savedHistory[0].Content);
        Assert.Equal("Posso ajudar com algo?", savedHistory[1].Content);
    }

    [Fact]
    public async Task ProcessWhatsAppMessageAsync_WhenDuplicateMessage_SkipsProcessingEntirely()
    {
        _historyMock.Setup(h => h.IsMessageDuplicateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _service.ProcessWhatsAppMessageAsync(ValidTenantId, ValidBotRequest());

        _customerMock.Verify(r => r.CreateAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()), Times.Never);
        _aiMock.Verify(ai => ai.ProcessUserMessageAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<List<MessageHistory>>()), Times.Never);
        _dispatcherMock.Verify(d => d.DispatchAsync(It.IsAny<GeminiIntentResponse>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessWhatsAppMessageAsync_WhenAiFails_ReturnsErrorStringWithoutCallingDispatcher()
    {
        _aiMock.Setup(ai => ai.ProcessUserMessageAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<List<MessageHistory>>()))
            .ThrowsAsync(new Exception("Gemini API error 429"));

        var reply = await _service.ProcessWhatsAppMessageAsync(ValidTenantId, ValidBotRequest());

        Assert.False(string.IsNullOrEmpty(reply));
        _dispatcherMock.Verify(d => d.DispatchAsync(It.IsAny<GeminiIntentResponse>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
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

        await _service.ProcessWhatsAppMessageAsync(ValidTenantId, ValidBotRequest());

        Assert.NotNull(passedHistory);
        Assert.Equal(2, passedHistory!.Count);
        Assert.Equal("Oi", passedHistory[0].Content);
    }

    // ── B22 — Registro automático de Customer ────────────────────────────────────

    [Fact]
    public async Task ProcessWhatsAppMessageAsync_WhenNewPhone_CreatesCustomerWithCorrectData()
    {
        var request = ValidBotRequest();
        _customerMock.Setup(r => r.GetByPhoneAndTenantAsync(request.NumeroRemetente, ValidTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        await _service.ProcessWhatsAppMessageAsync(ValidTenantId, request);

        _customerMock.Verify(r => r.CreateAsync(
            It.Is<Customer>(c =>
                c.PhoneNumber == request.NumeroRemetente &&
                c.Name        == request.NumeroRemetente &&
                c.TenantId    == ValidTenantId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessWhatsAppMessageAsync_WhenExistingCustomer_DoesNotCallCreate()
    {
        var request = ValidBotRequest();
        _customerMock.Setup(r => r.GetByPhoneAndTenantAsync(request.NumeroRemetente, ValidTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Customer
            {
                Name        = request.NumeroRemetente,
                PhoneNumber = request.NumeroRemetente,
                TenantId    = ValidTenantId
            });

        await _service.ProcessWhatsAppMessageAsync(ValidTenantId, request);

        _customerMock.Verify(r => r.CreateAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessWhatsAppMessageAsync_WhenNewPhone_ContinuesToProcessMessageAfterCreation()
    {
        _customerMock.Setup(r => r.GetByPhoneAndTenantAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        await _service.ProcessWhatsAppMessageAsync(ValidTenantId, ValidBotRequest());

        _aiMock.Verify(ai => ai.ProcessUserMessageAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<List<MessageHistory>>()), Times.Once);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────

    private static BotWebhookRequest ValidBotRequest() => new()
    {
        NumeroRemetente = "5511999999999",
        Texto           = "Olá, quero agendar"
    };
}
