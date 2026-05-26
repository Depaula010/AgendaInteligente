using AgendaInteligente.Api.Contracts.Models;
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
    private readonly Mock<IWebPushService>            _webPushMock;
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
        _webPushMock         = new Mock<IWebPushService>();

        // Default: customer existente, serviço "Corte", profissional "João"
        _customerMock.Setup(r => r.GetByPhoneAndTenantAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Customer { Id = Guid.NewGuid(), Name = SenderPhone, PhoneNumber = SenderPhone, TenantId = TenantId });

        _serviceMock.Setup(r => r.GetAllActiveByTenantAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Service> { new() { Id = Guid.NewGuid(), Name = "Corte", DurationMinutes = 30, Price = 40m, TenantId = TenantId } });

        _professionalMock.Setup(r => r.GetAllActiveByTenantAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Professional> { new() { Id = Guid.NewGuid(), Name = "João", Email = "joao@test.com", PasswordHash = "hash", TenantId = TenantId } });

        _scheduleServiceMock.Setup(s => s.CreateAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
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
    public async Task DispatchAsync_WithNonActionableIntent_ReturnsAiReplyUnchanged(string intent)
    {
        var ai = new GeminiIntentResponse { Intent = intent, ReplyMessage = "Resposta da IA" };

        var result = await _svc.DispatchAsync(ai, TenantId, SenderPhone);

        Assert.Equal("Resposta da IA", result.Text);
        Assert.False(result.HasInteractive);
        _scheduleServiceMock.Verify(s => s.CreateAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
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

        Assert.Equal("Para qual dia?", result.Text);
        _scheduleServiceMock.Verify(s => s.CreateAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
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

        Assert.Equal("Qual horário prefere?", result.Text);
        _scheduleServiceMock.Verify(s => s.CreateAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DispatchAsync_ScheduleIntent_MissingService_ReturnsInteractiveServiceList()
    {
        var ai = new GeminiIntentResponse
        {
            Intent = "schedule", Date = "2026-05-25", Time = "10:00",
            ReplyMessage = "Qual serviço?"
        };

        var result = await _svc.DispatchAsync(ai, TenantId, SenderPhone);

        Assert.True(result.HasInteractive);
        Assert.NotNull(result.InteractiveList);
        Assert.Equal("Serviços disponíveis", result.InteractiveList!.Title);
        var rows = result.InteractiveList.Sections.SelectMany(s => s.Rows).ToList();
        Assert.Contains(rows, r => r.Title == "Corte");
        _scheduleServiceMock.Verify(s => s.CreateAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DispatchAsync_ScheduleIntent_UnknownService_ReturnsInteractiveServiceList()
    {
        var ai = new GeminiIntentResponse
        {
            Intent = "schedule", Date = "2026-05-25", Time = "10:00",
            Service = "Massagem Relaxante",
            ReplyMessage = "Não encontrei esse serviço."
        };

        var result = await _svc.DispatchAsync(ai, TenantId, SenderPhone);

        Assert.True(result.HasInteractive);
        Assert.NotNull(result.InteractiveList);
        Assert.Equal("Serviços disponíveis", result.InteractiveList!.Title);
        Assert.Contains("Massagem Relaxante", result.InteractiveList.Body, StringComparison.OrdinalIgnoreCase);
        var rows = result.InteractiveList.Sections.SelectMany(s => s.Rows).ToList();
        Assert.Contains(rows, r => r.Title == "Corte");
        _scheduleServiceMock.Verify(s => s.CreateAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── schedule intent — lista de profissionais ─────────────────────────────────

    [Fact]
    public async Task DispatchAsync_ScheduleIntent_MultipleProfessionalsWithDateTime_ReturnsInteractiveProfessionalList()
    {
        var joaoId  = Guid.NewGuid();
        var pedroId = Guid.NewGuid();
        _professionalMock.Setup(r => r.GetAllActiveByTenantAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Professional>
            {
                new() { Id = joaoId,  Name = "João",  Email = "joao@test.com",  PasswordHash = "h", TenantId = TenantId },
                new() { Id = pedroId, Name = "Pedro", Email = "pedro@test.com", PasswordHash = "h", TenantId = TenantId }
            });
        _scheduleRepoMock.Setup(r => r.GetConflictingAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Schedule>());

        var ai = new GeminiIntentResponse
        {
            Intent = "schedule", Date = "2026-05-25", Time = "10:00",
            Service = "Corte", ReplyMessage = "Qual profissional você prefere?"
        };

        var result = await _svc.DispatchAsync(ai, TenantId, SenderPhone);

        Assert.True(result.HasInteractive);
        Assert.NotNull(result.InteractiveList);
        Assert.Equal("Escolha o profissional", result.InteractiveList!.Title);
        Assert.Contains("25/05", result.InteractiveList.Body);
        var rows = result.InteractiveList.Sections.SelectMany(s => s.Rows).ToList();
        Assert.Contains(rows, r => r.Title == "João");
        Assert.Contains(rows, r => r.Title == "Pedro");
        _scheduleServiceMock.Verify(s => s.CreateAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DispatchAsync_ScheduleIntent_MultipleProfessionalsNoDateTime_ReturnsInteractiveProfessionalList()
    {
        _professionalMock.Setup(r => r.GetAllActiveByTenantAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Professional>
            {
                new() { Id = Guid.NewGuid(), Name = "João",  Email = "joao@test.com",  PasswordHash = "h", TenantId = TenantId },
                new() { Id = Guid.NewGuid(), Name = "Pedro", Email = "pedro@test.com", PasswordHash = "h", TenantId = TenantId }
            });

        var ai = new GeminiIntentResponse
        {
            Intent = "schedule",
            Service = "Corte",  // sem data e hora
            ReplyMessage = "Qual profissional você prefere?"
        };

        var result = await _svc.DispatchAsync(ai, TenantId, SenderPhone);

        Assert.True(result.HasInteractive);
        Assert.NotNull(result.InteractiveList);
        Assert.Equal("Escolha o profissional", result.InteractiveList!.Title);
        var rows = result.InteractiveList.Sections.SelectMany(s => s.Rows).ToList();
        Assert.Contains(rows, r => r.Title == "João");
        Assert.Contains(rows, r => r.Title == "Pedro");
        _scheduleServiceMock.Verify(s => s.CreateAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
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

        Assert.Equal("Agendamento confirmado!", result.Text);
        _scheduleServiceMock.Verify(s => s.CreateAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Once);
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

        Assert.Equal("Agendado!", result.Text);
        _customerMock.Verify(r => r.CreateAsync(
            It.Is<Customer>(c => c.PhoneNumber == SenderPhone && c.TenantId == TenantId),
            It.IsAny<CancellationToken>()), Times.Once);
        _scheduleServiceMock.Verify(s => s.CreateAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── schedule intent — conflito → lista interativa (W29) ──────────────────────

    [Fact]
    public async Task DispatchAsync_ScheduleIntent_Conflict_ReturnsInteractiveListWithAlternatives()
    {
        var alternatives = new List<DateTime>
        {
            new(2026, 5, 25, 11, 0, 0, DateTimeKind.Utc),
            new(2026, 5, 25, 14, 0, 0, DateTimeKind.Utc)
        };
        SetupConflict(alternatives);

        var result = await _svc.DispatchAsync(FullScheduleIntent(), TenantId, SenderPhone);

        Assert.True(result.HasInteractive);
        Assert.NotNull(result.InteractiveList);
        Assert.Contains("ocupado", result.InteractiveList!.Body, StringComparison.OrdinalIgnoreCase);
        var rows = result.InteractiveList.Sections.SelectMany(s => s.Rows).ToList();
        Assert.Equal(2, rows.Count);
        Assert.Contains(rows, r => r.RowId == "2026-05-25 11:00");
        Assert.Contains(rows, r => r.RowId == "2026-05-25 14:00");
        Assert.Contains(rows, r => r.Title.Contains("11:00"));
        Assert.Contains(rows, r => r.Title.Contains("14:00"));
    }

    [Fact]
    public async Task DispatchAsync_ScheduleIntent_Conflict_RowIdContainsParsableDateTime()
    {
        var alt = new DateTime(2026, 6, 10, 9, 30, 0, DateTimeKind.Utc);
        SetupConflict(new List<DateTime> { alt });

        var result = await _svc.DispatchAsync(FullScheduleIntent(), TenantId, SenderPhone);

        Assert.True(result.HasInteractive);
        var row = result.InteractiveList!.Sections[0].Rows[0];
        Assert.Equal("2026-06-10 09:30", row.RowId);
        Assert.Contains("09:30", row.Title);
    }

    [Fact]
    public async Task DispatchAsync_ScheduleIntent_ConflictWithNoAlternatives_ReturnsTextMessage()
    {
        SetupConflict(new List<DateTime>());

        var result = await _svc.DispatchAsync(FullScheduleIntent(), TenantId, SenderPhone);

        Assert.False(result.HasInteractive);
        Assert.Contains("não encontrei horários disponíveis", result.Text, StringComparison.OrdinalIgnoreCase);
    }

    // ── cancel intent ────────────────────────────────────────────────────────────

    [Fact]
    public async Task DispatchAsync_CancelIntent_NoCustomer_ReturnsNotFoundMessage()
    {
        _customerMock.Setup(r => r.GetByPhoneAndTenantAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        var result = await _svc.DispatchAsync(new GeminiIntentResponse { Intent = "cancel", ReplyMessage = "Cancelando..." }, TenantId, SenderPhone);

        Assert.Contains("nenhum agendamento pendente", result.Text);
        _scheduleServiceMock.Verify(s => s.UpdateStatusAsync(
            It.IsAny<Guid>(), It.IsAny<ScheduleStatus>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DispatchAsync_CancelIntent_NoUpcomingAppointment_ReturnsNotFoundMessage()
    {
        _scheduleRepoMock.Setup(r => r.GetUpcomingByCustomerIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Schedule>());

        var result = await _svc.DispatchAsync(new GeminiIntentResponse { Intent = "cancel", ReplyMessage = "Cancelando..." }, TenantId, SenderPhone);

        Assert.Contains("nenhum agendamento pendente", result.Text);
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

        Assert.Contains("cancelado com sucesso", result.Text);
        Assert.Contains("26/05/2026", result.Text);
        Assert.Contains("09:00", result.Text);
        _scheduleServiceMock.Verify(s => s.UpdateStatusAsync(scheduleId, ScheduleStatus.Cancelled, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_CancelIntent_HasConfirmedAppointment_CancelsAndReturnsConfirmation()
    {
        var scheduleId  = Guid.NewGuid();
        var startTime   = new DateTime(2026, 5, 26, 14, 0, 0, DateTimeKind.Utc);
        _scheduleRepoMock.Setup(r => r.GetUpcomingByCustomerIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Schedule>
            {
                new()
                {
                    Id            = scheduleId,
                    ProfessionalId = Guid.NewGuid(),
                    StartDateTime = startTime,
                    EndDateTime   = startTime.AddHours(1),
                    Status        = ScheduleStatus.Confirmed,
                    Service       = new Service { Id = Guid.NewGuid(), Name = "Corte", DurationMinutes = 60, Price = 50m, TenantId = TenantId }
                }
            });
        _scheduleServiceMock.Setup(s => s.UpdateStatusAsync(It.IsAny<Guid>(), It.IsAny<ScheduleStatus>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _svc.DispatchAsync(new GeminiIntentResponse { Intent = "cancel", ReplyMessage = "Cancelando..." }, TenantId, SenderPhone);

        Assert.Contains("cancelado com sucesso", result.Text);
        Assert.Contains("Corte", result.Text);
        Assert.Contains("26/05/2026", result.Text);
        Assert.Contains("14:00", result.Text);
        _scheduleServiceMock.Verify(s => s.UpdateStatusAsync(scheduleId, ScheduleStatus.Cancelled, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── reschedule intent ────────────────────────────────────────────────────────

    [Fact]
    public async Task DispatchAsync_RescheduleIntent_MissingDateTime_ReturnsAiReply()
    {
        var ai = new GeminiIntentResponse
        {
            Intent = "reschedule", Service = "Corte",
            ReplyMessage = "Para qual dia e horário?"
        };

        var result = await _svc.DispatchAsync(ai, TenantId, SenderPhone);

        Assert.Equal("Para qual dia e horário?", result.Text);
        _scheduleServiceMock.Verify(s => s.CreateAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DispatchAsync_RescheduleIntent_NoCustomer_ReturnsNotFoundMessage()
    {
        _customerMock.Setup(r => r.GetByPhoneAndTenantAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        var result = await _svc.DispatchAsync(FullRescheduleIntent(), TenantId, SenderPhone);

        Assert.Contains("nenhum agendamento pendente", result.Text);
        _scheduleServiceMock.Verify(s => s.CreateAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DispatchAsync_RescheduleIntent_NoUpcomingAppointment_ReturnsNotFoundMessage()
    {
        _scheduleRepoMock.Setup(r => r.GetUpcomingByCustomerIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Schedule>());

        var result = await _svc.DispatchAsync(FullRescheduleIntent(), TenantId, SenderPhone);

        Assert.Contains("nenhum agendamento pendente", result.Text);
        _scheduleServiceMock.Verify(s => s.CreateAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DispatchAsync_RescheduleIntent_HappyPath_CancelsOldCreatesNewReturnsConfirmation()
    {
        var oldId    = Guid.NewGuid();
        var profId   = Guid.NewGuid();
        var svcId    = Guid.NewGuid();
        var oldStart = new DateTime(2026, 5, 26, 9, 0, 0, DateTimeKind.Utc);
        var newStart = new DateTime(2026, 5, 28, 10, 0, 0, DateTimeKind.Utc);

        _scheduleRepoMock.Setup(r => r.GetUpcomingByCustomerIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Schedule>
            {
                new()
                {
                    Id             = oldId,
                    ProfessionalId = profId,
                    StartDateTime  = oldStart,
                    EndDateTime    = oldStart.AddMinutes(30),
                    Status         = ScheduleStatus.Pending,
                    Service        = new Service      { Id = svcId,  Name = "Corte", DurationMinutes = 30, Price = 40m, TenantId = TenantId },
                    Professional   = new Professional { Id = profId, Name = "João",  Email = "joao@test.com", PasswordHash = "hash", TenantId = TenantId }
                }
            });
        _scheduleServiceMock.Setup(s => s.CreateAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), svcId,
                It.Is<DateTime>(d => d == newStart), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Schedule { ProfessionalId = profId, StartDateTime = newStart, EndDateTime = newStart.AddMinutes(30) });
        _scheduleServiceMock.Setup(s => s.UpdateStatusAsync(oldId, ScheduleStatus.Cancelled, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _svc.DispatchAsync(FullRescheduleIntent(newStart), TenantId, SenderPhone);

        Assert.Contains("remarcado", result.Text);
        Assert.Contains("Corte", result.Text);
        Assert.Contains("28/05/2026", result.Text);
        Assert.Contains("10:00", result.Text);
        _scheduleServiceMock.Verify(s => s.CreateAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), svcId,
            It.Is<DateTime>(d => d == newStart), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Once);
        _scheduleServiceMock.Verify(s => s.UpdateStatusAsync(oldId, ScheduleStatus.Cancelled, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_RescheduleIntent_NewSlotConflicts_DoesNotCancelOldAndReturnsConflictMessage()
    {
        var oldId    = Guid.NewGuid();
        var profId   = Guid.NewGuid();
        var svcId    = Guid.NewGuid();
        var oldStart = new DateTime(2026, 5, 26, 9, 0, 0, DateTimeKind.Utc);
        var newStart = new DateTime(2026, 5, 28, 10, 0, 0, DateTimeKind.Utc);

        _scheduleRepoMock.Setup(r => r.GetUpcomingByCustomerIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Schedule>
            {
                new()
                {
                    Id             = oldId,
                    ProfessionalId = profId,
                    StartDateTime  = oldStart,
                    EndDateTime    = oldStart.AddMinutes(30),
                    Service        = new Service      { Id = svcId,  Name = "Corte", DurationMinutes = 30, Price = 40m, TenantId = TenantId },
                    Professional   = new Professional { Id = profId, Name = "João",  Email = "joao@test.com", PasswordHash = "hash", TenantId = TenantId }
                }
            });
        var alternatives = new List<DateTime>
        {
            new(2026, 5, 28, 11, 0, 0, DateTimeKind.Utc),
            new(2026, 5, 28, 14, 0, 0, DateTimeKind.Utc)
        };
        _scheduleServiceMock.Setup(s => s.CreateAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ScheduleConflictException("Horário ocupado.", alternatives));

        var result = await _svc.DispatchAsync(FullRescheduleIntent(newStart), TenantId, SenderPhone);

        Assert.True(result.HasInteractive);
        var rows = result.InteractiveList!.Sections.SelectMany(s => s.Rows).ToList();
        Assert.Contains(rows, r => r.Title.Contains("11:00"));
        _scheduleServiceMock.Verify(s => s.UpdateStatusAsync(
            It.IsAny<Guid>(), It.IsAny<ScheduleStatus>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── helpers ──────────────────────────────────────────────────────────────────

    private BotIntentDispatcherService BuildService() => new(
        _customerMock.Object,
        _serviceMock.Object,
        _professionalMock.Object,
        _scheduleRepoMock.Object,
        _scheduleServiceMock.Object,
        _settingsMock.Object,
        _webPushMock.Object,
        new NullLogger<BotIntentDispatcherService>());

    private static GeminiIntentResponse FullScheduleIntent() => new()
    {
        Intent = "schedule", Date = "2026-05-25", Time = "10:00",
        Service = "Corte", ReplyMessage = "Agendado!"
    };

    private static GeminiIntentResponse FullRescheduleIntent(DateTime? newStart = null)
    {
        var dt = newStart ?? new DateTime(2026, 5, 28, 10, 0, 0, DateTimeKind.Utc);
        return new GeminiIntentResponse
        {
            Intent       = "reschedule",
            Date         = dt.ToString("yyyy-MM-dd"),
            Time         = dt.ToString("HH:mm"),
            ReplyMessage = "Remarcando..."
        };
    }

    private void SetupConflict(List<DateTime> alternatives)
        => _scheduleServiceMock.Setup(s => s.CreateAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ScheduleConflictException("Horário ocupado.", alternatives));
}
