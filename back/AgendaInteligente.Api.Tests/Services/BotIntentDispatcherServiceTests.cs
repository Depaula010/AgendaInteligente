using AgendaInteligente.Api.Domain.Entities;
using AgendaInteligente.Api.Domain.Enums;
using AgendaInteligente.Api.Domain.Exceptions;
using AgendaInteligente.Api.Models.AI;
using AgendaInteligente.Api.Repositories.Interfaces;
using AgendaInteligente.Api.Services;
using AgendaInteligente.Api.Services.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AgendaInteligente.Api.Tests.Services;

public sealed class BotIntentDispatcherServiceTests
{
    private readonly Mock<ICustomerRepository>        _customerMock;
    private readonly Mock<IServiceCatalogRepository>  _serviceMock;
    private readonly Mock<IProfessionalRepository>    _professionalMock;
    private readonly Mock<IScheduleRepository>        _scheduleRepoMock;
    private readonly Mock<IScheduleService>           _scheduleServiceMock;
    private readonly Mock<ITenantSettingsRepository>  _settingsMock;
    private readonly BotIntentDispatcherService       _svc;

    private static readonly Guid   TenantId    = Guid.NewGuid();
    private const           string SenderPhone = "5511999999999";

    public BotIntentDispatcherServiceTests()
    {
        _customerMock        = new Mock<ICustomerRepository>();
        _serviceMock         = new Mock<IServiceCatalogRepository>();
        _professionalMock    = new Mock<IProfessionalRepository>();
        _scheduleRepoMock    = new Mock<IScheduleRepository>();
        _scheduleServiceMock = new Mock<IScheduleService>();
        _settingsMock        = new Mock<ITenantSettingsRepository>();

        // Default: customer existente, serviço "Corte", profissional "João"
        _customerMock.Setup(r => r.GetByPhoneAndTenantAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Customer { Id = Guid.NewGuid(), Name = SenderPhone, PhoneNumber = SenderPhone, TenantId = TenantId });

        _serviceMock.Setup(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Service> { new() { Id = Guid.NewGuid(), Name = "Corte", DurationMinutes = 30, Price = 40m, TenantId = TenantId } });

        _professionalMock.Setup(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Professional> { new() { Id = Guid.NewGuid(), Name = "João", Email = "joao@test.com", PasswordHash = "hash", TenantId = TenantId } });

        _scheduleServiceMock.Setup(s => s.CreateAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Schedule { ProfessionalId = Guid.NewGuid(), StartDateTime = DateTime.UtcNow.AddDays(1), EndDateTime = DateTime.UtcNow.AddDays(1).AddMinutes(30) });

        // Default: settings sem template personalizado (usa o default do serviço)
        _settingsMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantSettings?)null);

        _svc = BuildService();
    }

    // ── intents sem ação de domínio ──────────────────────────────────────────────

    [Theory]
    [InlineData("general")]
    [InlineData("check")]
    [InlineData("reschedule")]
    public async Task DispatchAsync_WithNonActionableIntent_ReturnsAiReplyUnchanged(string intent)
    {
        var ai = new GeminiIntentResponse { Intent = intent, ReplyMessage = "Resposta da IA" };

        var result = await _svc.DispatchAsync(ai, TenantId, SenderPhone);

        Assert.Equal("Resposta da IA", result);
        _scheduleServiceMock.Verify(s => s.CreateAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── schedule intent — dados incompletos ──────────────────────────────────────

    [Fact]
    public async Task DispatchAsync_ScheduleIntent_MissingDate_ReturnsAiReply()
    {
        var ai = new GeminiIntentResponse
        {
            Intent = "schedule", Time = "10:00", Service = "Corte",
            ReplyMessage = "Para qual dia?"
        };

        var result = await _svc.DispatchAsync(ai, TenantId, SenderPhone);

        Assert.Equal("Para qual dia?", result);
        _scheduleServiceMock.Verify(s => s.CreateAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DispatchAsync_ScheduleIntent_MissingTime_ReturnsAiReply()
    {
        var ai = new GeminiIntentResponse
        {
            Intent = "schedule", Date = "2026-05-25", Service = "Corte",
            ReplyMessage = "Qual horário prefere?"
        };

        var result = await _svc.DispatchAsync(ai, TenantId, SenderPhone);

        Assert.Equal("Qual horário prefere?", result);
        _scheduleServiceMock.Verify(s => s.CreateAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DispatchAsync_ScheduleIntent_MissingService_ReturnsAiReply()
    {
        var ai = new GeminiIntentResponse
        {
            Intent = "schedule", Date = "2026-05-25", Time = "10:00",
            ReplyMessage = "Qual serviço?"
        };

        var result = await _svc.DispatchAsync(ai, TenantId, SenderPhone);

        Assert.Equal("Qual serviço?", result);
        _scheduleServiceMock.Verify(s => s.CreateAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DispatchAsync_ScheduleIntent_UnknownService_ReturnsAiReply()
    {
        var ai = new GeminiIntentResponse
        {
            Intent = "schedule", Date = "2026-05-25", Time = "10:00",
            Service = "Massagem Relaxante",
            ReplyMessage = "Não encontrei esse serviço."
        };

        var result = await _svc.DispatchAsync(ai, TenantId, SenderPhone);

        Assert.Equal("Não encontrei esse serviço.", result);
        _scheduleServiceMock.Verify(s => s.CreateAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── schedule intent — sucesso ────────────────────────────────────────────────

    [Fact]
    public async Task DispatchAsync_ScheduleIntent_AllDataPresent_CreatesScheduleAndReturnsAiReply()
    {
        var ai = new GeminiIntentResponse
        {
            Intent = "schedule", Date = "2026-05-25", Time = "10:00",
            Service = "Corte", Professional = "João",
            ReplyMessage = "Agendamento confirmado!"
        };

        var result = await _svc.DispatchAsync(ai, TenantId, SenderPhone);

        Assert.Equal("Agendamento confirmado!", result);
        _scheduleServiceMock.Verify(s => s.CreateAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_ScheduleIntent_NoExistingCustomer_CreatesCustomerThenSchedule()
    {
        _customerMock.Setup(r => r.GetByPhoneAndTenantAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);
        _customerMock.Setup(r => r.CreateAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer c, CancellationToken _) => c);

        var ai = new GeminiIntentResponse
        {
            Intent = "schedule", Date = "2026-05-25", Time = "10:00",
            Service = "Corte", ReplyMessage = "Agendado!"
        };

        var result = await _svc.DispatchAsync(ai, TenantId, SenderPhone);

        Assert.Equal("Agendado!", result);
        _customerMock.Verify(r => r.CreateAsync(
            It.Is<Customer>(c => c.PhoneNumber == SenderPhone && c.TenantId == TenantId),
            It.IsAny<CancellationToken>()), Times.Once);
        _scheduleServiceMock.Verify(s => s.CreateAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── schedule intent — conflito com template padrão ───────────────────────────

    [Fact]
    public async Task DispatchAsync_ScheduleIntent_Conflict_UsesDefaultTemplateWhenSettingsNull()
    {
        // Arrange — settings não configurado (null): deve usar DefaultConflictTemplate
        _settingsMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantSettings?)null);

        var alternatives = new List<DateTime>
        {
            new(2026, 5, 25, 11, 0, 0, DateTimeKind.Utc),
            new(2026, 5, 25, 14, 0, 0, DateTimeKind.Utc)
        };
        _scheduleServiceMock.Setup(s => s.CreateAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ScheduleConflictException("Horário ocupado.", alternatives));

        var ai = FullScheduleIntent();

        var result = await _svc.DispatchAsync(ai, TenantId, SenderPhone);

        Assert.Contains("ocupado", result);
        Assert.Contains("11:00", result);
        Assert.Contains("14:00", result);
        Assert.Contains("Qual desses horários prefere?", result);
        // Garante que foi ao banco buscar settings
        _settingsMock.Verify(r => r.GetAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_ScheduleIntent_Conflict_UsesDefaultTemplateWhenTemplateIsNull()
    {
        // Settings existe mas ConflictMessageTemplate não foi configurado
        _settingsMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TenantSettings { ConflictMessageTemplate = null });

        var alternatives = new List<DateTime> { new(2026, 5, 25, 11, 0, 0, DateTimeKind.Utc) };
        SetupConflict(alternatives);

        var result = await _svc.DispatchAsync(FullScheduleIntent(), TenantId, SenderPhone);

        Assert.Contains(BotIntentDispatcherService.DefaultConflictTemplate
            .Replace("{alternatives}", "• 25/05/2026 às 11:00").Substring(0, 30), result);
    }

    [Fact]
    public async Task DispatchAsync_ScheduleIntent_Conflict_UsesCustomTemplateFromSettings()
    {
        // Tenant configurou seu próprio template
        const string customTemplate = "Opa, esse horário tá cheio! 🔥 Veja as vagas:\n{alternatives}\nFica um?";
        _settingsMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TenantSettings { ConflictMessageTemplate = customTemplate });

        var alternatives = new List<DateTime>
        {
            new(2026, 5, 25, 11, 0, 0, DateTimeKind.Utc),
            new(2026, 5, 25, 14, 0, 0, DateTimeKind.Utc)
        };
        SetupConflict(alternatives);

        var result = await _svc.DispatchAsync(FullScheduleIntent(), TenantId, SenderPhone);

        Assert.Contains("cheio! 🔥", result);
        Assert.Contains("11:00", result);
        Assert.Contains("14:00", result);
        Assert.Contains("Fica um?", result);
        // Template padrão NÃO deve aparecer
        Assert.DoesNotContain("Qual desses horários prefere?", result);
    }

    [Fact]
    public async Task DispatchAsync_ScheduleIntent_ConflictWithNoAlternatives_ReturnsNoAlternativesMessage()
    {
        SetupConflict(new List<DateTime>());

        var result = await _svc.DispatchAsync(FullScheduleIntent(), TenantId, SenderPhone);

        Assert.Contains("não encontrei horários disponíveis", result, StringComparison.OrdinalIgnoreCase);
        // Sem alternativas: não deve chamar settings
        _settingsMock.Verify(r => r.GetAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── cancel intent ────────────────────────────────────────────────────────────

    [Fact]
    public async Task DispatchAsync_CancelIntent_NoCustomer_ReturnsNotFoundMessage()
    {
        _customerMock.Setup(r => r.GetByPhoneAndTenantAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        var result = await _svc.DispatchAsync(new GeminiIntentResponse { Intent = "cancel", ReplyMessage = "Cancelando..." }, TenantId, SenderPhone);

        Assert.Contains("nenhum agendamento pendente", result);
        _scheduleServiceMock.Verify(s => s.UpdateStatusAsync(
            It.IsAny<Guid>(), It.IsAny<ScheduleStatus>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DispatchAsync_CancelIntent_NoUpcomingAppointment_ReturnsNotFoundMessage()
    {
        _scheduleRepoMock.Setup(r => r.GetUpcomingByCustomerIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Schedule>());

        var result = await _svc.DispatchAsync(new GeminiIntentResponse { Intent = "cancel", ReplyMessage = "Cancelando..." }, TenantId, SenderPhone);

        Assert.Contains("nenhum agendamento pendente", result);
        _scheduleServiceMock.Verify(s => s.UpdateStatusAsync(
            It.IsAny<Guid>(), It.IsAny<ScheduleStatus>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DispatchAsync_CancelIntent_HasUpcomingAppointment_CancelsNextAndReturnsConfirmation()
    {
        var scheduleId = Guid.NewGuid();
        var startTime  = new DateTime(2026, 5, 26, 9, 0, 0, DateTimeKind.Utc);
        _scheduleRepoMock.Setup(r => r.GetUpcomingByCustomerIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Schedule>
            {
                new() { Id = scheduleId, ProfessionalId = Guid.NewGuid(), StartDateTime = startTime, EndDateTime = startTime.AddMinutes(30) }
            });
        _scheduleServiceMock.Setup(s => s.UpdateStatusAsync(It.IsAny<Guid>(), It.IsAny<ScheduleStatus>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _svc.DispatchAsync(new GeminiIntentResponse { Intent = "cancel", ReplyMessage = "Cancelando..." }, TenantId, SenderPhone);

        Assert.Contains("cancelado com sucesso", result);
        Assert.Contains("26/05/2026", result);
        Assert.Contains("09:00", result);
        _scheduleServiceMock.Verify(s => s.UpdateStatusAsync(scheduleId, ScheduleStatus.Cancelled, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── helpers ──────────────────────────────────────────────────────────────────

    private BotIntentDispatcherService BuildService() => new(
        _customerMock.Object,
        _serviceMock.Object,
        _professionalMock.Object,
        _scheduleRepoMock.Object,
        _scheduleServiceMock.Object,
        _settingsMock.Object,
        new NullLogger<BotIntentDispatcherService>());

    private static GeminiIntentResponse FullScheduleIntent() => new()
    {
        Intent = "schedule", Date = "2026-05-25", Time = "10:00",
        Service = "Corte", ReplyMessage = "Agendado!"
    };

    private void SetupConflict(List<DateTime> alternatives)
        => _scheduleServiceMock.Setup(s => s.CreateAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ScheduleConflictException("Horário ocupado.", alternatives));
}
