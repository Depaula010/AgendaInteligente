using AgendaInteligente.Api.Domain.Entities;
using AgendaInteligente.Api.Domain.Enums;
using AgendaInteligente.Api.Domain.Exceptions;
using AgendaInteligente.Api.Repositories.Interfaces;
using AgendaInteligente.Api.Services;
using AgendaInteligente.Api.Services.CalendarSync;
using AgendaInteligente.Api.Services.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AgendaInteligente.Api.Tests.Services;

public sealed class ScheduleServiceTests
{
    // ── Fixtures ───────────────────────────────────────────────────────────────
    private readonly Mock<IScheduleRepository>       _scheduleRepoMock = new();
    private readonly Mock<IServiceCatalogRepository> _serviceRepoMock  = new();
    private readonly Mock<ITenantSettingsRepository> _settingsRepoMock = new();
    private readonly Mock<ICalendarSyncQueue>        _syncQueueMock    = new();
    private readonly Mock<IWaitlistService>          _waitlistSvcMock  = new();
    private readonly ScheduleService _sut;

    // IDs fixos para legibilidade dos testes
    private static readonly Guid CustomerId     = Guid.NewGuid();
    private static readonly Guid ProfessionalId = Guid.NewGuid();
    private static readonly Guid ServiceId      = Guid.NewGuid();

    // Serviço padrão: 60 minutos de duração
    private static readonly Service DefaultService = new()
    {
        Id              = ServiceId,
        Name            = "Corte",
        DurationMinutes = 60,
        Price           = 50m,
        TenantId        = Guid.NewGuid()
    };

    public ScheduleServiceTests()
    {
        // Configura o mock da waitlist para não fazer nada por padrão (não afeta testes existentes)
        _waitlistSvcMock
            .Setup(w => w.ProcessCancellationAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), default))
            .Returns(Task.CompletedTask);

        _sut = new ScheduleService(
            _scheduleRepoMock.Object,
            _serviceRepoMock.Object,
            _settingsRepoMock.Object,
            _syncQueueMock.Object,
            _waitlistSvcMock.Object,
            NullLogger<ScheduleService>.Instance);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private void SetupServiceExists(Service? service = null)
        => _serviceRepoMock
            .Setup(r => r.GetByIdAsync(ServiceId, default))
            .ReturnsAsync(service ?? DefaultService);

    private void SetupNoConflicts()
        => _scheduleRepoMock
            .Setup(r => r.GetConflictingAsync(ProfessionalId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), default))
            .ReturnsAsync([]);

    private void SetupConflicts(IReadOnlyList<Schedule> conflicts)
        => _scheduleRepoMock
            .Setup(r => r.GetConflictingAsync(ProfessionalId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), default))
            .ReturnsAsync(conflicts);

    private void SetupCreateReturnsEntity()
        => _scheduleRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<Schedule>(), default))
            .ReturnsAsync((Schedule s, CancellationToken _) => s);

    // ── CreateAsync: cenários de sucesso ───────────────────────────────────────

    [Fact]
    public async Task CreateAsync_WithNoConflict_ReturnsCreatedSchedule()
    {
        // Arrange
        var start = new DateTime(2026, 6, 10, 9, 0, 0, DateTimeKind.Utc);
        SetupServiceExists();
        SetupNoConflicts();
        SetupCreateReturnsEntity();

        // Act
        var result = await _sut.CreateAsync(CustomerId, ProfessionalId, ServiceId, start);

        // Assert
        Assert.Equal(CustomerId,     result.CustomerId);
        Assert.Equal(ProfessionalId, result.ProfessionalId);
        Assert.Equal(ServiceId,      result.ServiceId);
        Assert.Equal(start,          result.StartDateTime);
        Assert.Equal(ScheduleStatus.Pending, result.Status);
    }

    [Fact]
    public async Task CreateAsync_CalculatesEndDateTimeFromServiceDuration()
    {
        // Arrange — serviço de 90 minutos
        var service90 = new Service { Id = ServiceId, DurationMinutes = 90, Name = "Barba+Corte", Price = 80m, TenantId = Guid.NewGuid() };
        var start = new DateTime(2026, 6, 10, 9, 0, 0, DateTimeKind.Utc);

        SetupServiceExists(service90);
        SetupNoConflicts();
        SetupCreateReturnsEntity();

        // Act
        var result = await _sut.CreateAsync(CustomerId, ProfessionalId, ServiceId, start);

        // Assert — EndDateTime deve ser exatamente 90 min após o início
        Assert.Equal(start.AddMinutes(90), result.EndDateTime);
    }

    [Fact]
    public async Task CreateAsync_WithLocalDateTime_NormalizesToUtc()
    {
        // Arrange
        var localStart = new DateTime(2026, 6, 10, 9, 0, 0, DateTimeKind.Local);
        SetupServiceExists();
        SetupNoConflicts();
        SetupCreateReturnsEntity();

        // Act
        var result = await _sut.CreateAsync(CustomerId, ProfessionalId, ServiceId, localStart);

        // Assert — a data armazenada deve ser UTC
        Assert.Equal(DateTimeKind.Utc, result.StartDateTime.Kind);
    }

    [Fact]
    public async Task CreateAsync_CallsRepositoryCreate_ExactlyOnce()
    {
        // Arrange
        var start = new DateTime(2026, 6, 10, 9, 0, 0, DateTimeKind.Utc);
        SetupServiceExists();
        SetupNoConflicts();
        SetupCreateReturnsEntity();

        // Act
        await _sut.CreateAsync(CustomerId, ProfessionalId, ServiceId, start);

        // Assert
        _scheduleRepoMock.Verify(r => r.CreateAsync(It.IsAny<Schedule>(), default), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_CallsSyncQueue_ExactlyOnce()
    {
        // Arrange
        var start = new DateTime(2026, 6, 10, 9, 0, 0, DateTimeKind.Utc);
        SetupServiceExists();
        SetupNoConflicts();
        SetupCreateReturnsEntity();

        // Act
        await _sut.CreateAsync(CustomerId, ProfessionalId, ServiceId, start);

        // Assert
        _syncQueueMock.Verify(q => q.EnqueueAsync(
            It.Is<CalendarSyncMessage>(m => m.Operation == CalendarSyncOperation.Upsert), 
            default), Times.Once);
    }

    // ── CreateAsync: conflito → ScheduleConflictException com alternativas ─────

    [Fact]
    public async Task CreateAsync_WhenConflictExists_ThrowsScheduleConflictException()
    {
        // Arrange — slot de 10h–11h ocupado; agenda livre nas demais horas do dia
        var start = new DateTime(2026, 6, 10, 10, 0, 0, DateTimeKind.Utc);
        var conflicting = new Schedule
        {
            Id             = Guid.NewGuid(),
            ProfessionalId = ProfessionalId,
            StartDateTime  = start,
            EndDateTime    = start.AddMinutes(60)
        };

        SetupServiceExists();

        // Primeira chamada: conflito no horário solicitado
        // Demais chamadas (candidatos alternativos): sem conflito
        _scheduleRepoMock
            .SetupSequence(r => r.GetConflictingAsync(ProfessionalId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), default))
            .ReturnsAsync([conflicting])   // slot solicitado — conflito
            .ReturnsAsync([])              // 1.º candidato alternativo
            .ReturnsAsync([])              // 2.º candidato alternativo
            .ReturnsAsync([]);             // 3.º candidato alternativo

        // Act & Assert — deve lançar ScheduleConflictException (não InvalidOperationException genérica)
        var ex = await Assert.ThrowsAsync<ScheduleConflictException>(
            () => _sut.CreateAsync(CustomerId, ProfessionalId, ServiceId, start));

        Assert.NotNull(ex.SuggestedAlternatives);
        // Garante que pelo menos 1 alternativa foi encontrada (não vazio)
        Assert.NotEmpty(ex.SuggestedAlternatives);
        // Garante que as alternativas são datas futuras em UTC
        Assert.All(ex.SuggestedAlternatives, dt => Assert.Equal(DateTimeKind.Utc, dt.Kind));
    }

    [Fact]
    public async Task CreateAsync_WhenConflictExists_SuggestedAlternatives_DoNotIncludeConflictedSlot_WhenItRemainsBlocked()
    {
        // Arrange — slot de 10h bloqueado em TODAS as consultas (incluindo busca de alternativas)
        // Slots de 09h30 e 10h30 estão livres
        var start = new DateTime(2026, 6, 10, 10, 0, 0, DateTimeKind.Utc);
        var conflicting = new Schedule { Id = Guid.NewGuid(), ProfessionalId = ProfessionalId };

        SetupServiceExists();

        // 10:00–11:00 conflita sempre (slot principal e quando avaliado na busca de alternativas)
        _scheduleRepoMock
            .Setup(r => r.GetConflictingAsync(ProfessionalId,
                new DateTime(2026, 6, 10, 10, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 6, 10, 11, 0, 0, DateTimeKind.Utc),
                default))
            .ReturnsAsync([conflicting]);

        // Todos os outros slots livres
        _scheduleRepoMock
            .Setup(r => r.GetConflictingAsync(ProfessionalId,
                It.Is<DateTime>(d => d != new DateTime(2026, 6, 10, 10, 0, 0, DateTimeKind.Utc)),
                It.IsAny<DateTime>(),
                default))
            .ReturnsAsync([]);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ScheduleConflictException>(
            () => _sut.CreateAsync(CustomerId, ProfessionalId, ServiceId, start));

        // As alternativas não devem conter 10:00 (que permaneceu bloqueado)
        Assert.DoesNotContain(new DateTime(2026, 6, 10, 10, 0, 0, DateTimeKind.Utc), ex.SuggestedAlternatives);
        Assert.NotEmpty(ex.SuggestedAlternatives);
    }


    [Fact]
    public async Task CreateAsync_WhenConflictExists_DoesNotCallRepositoryCreate()
    {
        // Arrange
        var start = new DateTime(2026, 6, 10, 10, 0, 0, DateTimeKind.Utc);
        SetupServiceExists();

        // Todos os slots retornam conflito para simplificar o mock
        _scheduleRepoMock
            .Setup(r => r.GetConflictingAsync(ProfessionalId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), default))
            .ReturnsAsync([new Schedule { Id = Guid.NewGuid() }]);

        // Act
        await Assert.ThrowsAsync<ScheduleConflictException>(
            () => _sut.CreateAsync(CustomerId, ProfessionalId, ServiceId, start));

        // Assert — nenhum insert deve ter ocorrido
        _scheduleRepoMock.Verify(r => r.CreateAsync(It.IsAny<Schedule>(), default), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenServiceNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange — serviço não existe (inativo ou removido)
        _serviceRepoMock
            .Setup(r => r.GetByIdAsync(ServiceId, default))
            .ReturnsAsync((Service?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.CreateAsync(CustomerId, ProfessionalId, ServiceId,
                                   new DateTime(2026, 6, 10, 9, 0, 0, DateTimeKind.Utc)));
    }

    // ── Lógica de sobreposição de intervalos ───────────────────────────────────
    // Garantia de que o algoritmo cobre todos os casos de Allen's Interval Algebra

    [Theory]
    [MemberData(nameof(OverlappingIntervals))]
    public async Task CreateAsync_OverlappingInterval_ThrowsScheduleConflictException(DateTime existStart, DateTime existEnd)
    {
        // Novo agendamento: 10h–11h
        var newStart = new DateTime(2026, 6, 10, 10, 0, 0, DateTimeKind.Utc);
        var existing = new Schedule
        {
            Id             = Guid.NewGuid(),
            ProfessionalId = ProfessionalId,
            StartDateTime  = existStart,
            EndDateTime    = existEnd
        };

        SetupServiceExists(); // 60 min → 10h–11h

        // Primeira chamada (slot solicitado): conflito
        // Demais (candidatos): sem conflito
        _scheduleRepoMock
            .SetupSequence(r => r.GetConflictingAsync(ProfessionalId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), default))
            .ReturnsAsync([existing])
            .ReturnsAsync([])
            .ReturnsAsync([])
            .ReturnsAsync([]);

        await Assert.ThrowsAsync<ScheduleConflictException>(
            () => _sut.CreateAsync(CustomerId, ProfessionalId, ServiceId, newStart));
    }

    public static TheoryData<DateTime, DateTime> OverlappingIntervals()
    {
        var d = new DateTime(2026, 6, 10, 0, 0, 0, DateTimeKind.Utc);
        return new TheoryData<DateTime, DateTime>
        {
            { d.AddHours(9),  d.AddHours(11) },  // existente engloba o novo
            { d.AddHours(10), d.AddHours(12) },  // começa junto, termina depois
            { d.AddHours(9),  d.AddHours(10).AddMinutes(30) }, // início antes, sobreposição parcial
            { d.AddHours(10).AddMinutes(15), d.AddHours(10).AddMinutes(45) }, // totalmente dentro
        };
    }

    // ── GetAlternativeTimesAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetAlternativeTimesAsync_WhenDayHasAvailableSlots_ReturnsClosestAlternatives()
    {
        // Arrange — profissional com 1 conflito no horário pedido, demais livres
        // Solicitado: 10h00; esperamos que os mais próximos (09h30 ou 10h30) sejam sugeridos
        var requested = new DateTime(2099, 6, 10, 10, 0, 0, DateTimeKind.Utc); // futuro garantido

        SetupServiceExists(); // 60 min

        // Todos os slots livres, exceto o exato das 10h
        _scheduleRepoMock
            .Setup(r => r.GetConflictingAsync(ProfessionalId,
                new DateTime(2099, 6, 10, 10, 0, 0, DateTimeKind.Utc),
                new DateTime(2099, 6, 10, 11, 0, 0, DateTimeKind.Utc),
                default))
            .ReturnsAsync([new Schedule { Id = Guid.NewGuid() }]);

        _scheduleRepoMock
            .Setup(r => r.GetConflictingAsync(ProfessionalId,
                It.IsNotIn(new DateTime(2099, 6, 10, 10, 0, 0, DateTimeKind.Utc)),
                It.IsAny<DateTime>(),
                default))
            .ReturnsAsync([]);

        // Act
        var alternatives = await _sut.GetAlternativeTimesAsync(ProfessionalId, ServiceId, requested, count: 3);

        // Assert
        Assert.NotEmpty(alternatives);
        Assert.True(alternatives.Count <= 3);
        Assert.All(alternatives, dt =>
        {
            Assert.Equal(DateTimeKind.Utc, dt.Kind);
            Assert.NotEqual(requested, dt);
        });
    }

    [Fact]
    public async Task GetAlternativeTimesAsync_WhenCurrentDayIsFullyBooked_ReturnsFromNextDays()
    {
        // Arrange — dia solicitado completamente bloqueado, próximo dia livre
        var requested = new DateTime(2099, 7, 1, 10, 0, 0, DateTimeKind.Utc);

        SetupServiceExists(); // 60 min

        var requestedDate = requested.Date;

        // Todos os slots do dia D bloqueados
        _scheduleRepoMock
            .Setup(r => r.GetConflictingAsync(ProfessionalId,
                It.Is<DateTime>(d => d.Date == requestedDate),
                It.IsAny<DateTime>(),
                default))
            .ReturnsAsync([new Schedule { Id = Guid.NewGuid() }]);

        // Dia D+1 completamente livre
        _scheduleRepoMock
            .Setup(r => r.GetConflictingAsync(ProfessionalId,
                It.Is<DateTime>(d => d.Date == requestedDate.AddDays(1)),
                It.IsAny<DateTime>(),
                default))
            .ReturnsAsync([]);

        // Act
        var alternatives = await _sut.GetAlternativeTimesAsync(ProfessionalId, ServiceId, requested, count: 3, maxSearchDays: 7);

        // Assert — deve ter encontrado alternativas no D+1
        Assert.NotEmpty(alternatives);
        Assert.All(alternatives, dt => Assert.True(dt.Date >= requestedDate.AddDays(1)));
    }

    [Fact]
    public async Task GetAlternativeTimesAsync_WhenNoSlotsFoundInWindow_ReturnsEmptyList()
    {
        // Arrange — toda a janela de busca está bloqueada
        var requested = new DateTime(2099, 8, 1, 10, 0, 0, DateTimeKind.Utc);

        SetupServiceExists();

        // Todos os slots conflitam (agenda completamente cheia)
        _scheduleRepoMock
            .Setup(r => r.GetConflictingAsync(ProfessionalId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), default))
            .ReturnsAsync([new Schedule { Id = Guid.NewGuid() }]);

        // Act
        var alternatives = await _sut.GetAlternativeTimesAsync(ProfessionalId, ServiceId, requested, count: 3, maxSearchDays: 2);

        // Assert — nenhuma alternativa disponível
        Assert.Empty(alternatives);
    }

    [Fact]
    public async Task GetAlternativeTimesAsync_ShouldNotSuggestPastSlots()
    {
        // Arrange — data no passado para garantir que "candidate < UtcNow" filtra tudo
        var pastRequested = new DateTime(2000, 1, 1, 10, 0, 0, DateTimeKind.Utc);

        SetupServiceExists();
        SetupNoConflicts(); // sem conflitos no mock, mas todos os slots são passado

        // Act
        var alternatives = await _sut.GetAlternativeTimesAsync(ProfessionalId, ServiceId, pastRequested, count: 3, maxSearchDays: 1);

        // Assert — nenhum slot passado deve ser sugerido
        Assert.Empty(alternatives);
    }

    [Fact]
    public async Task GetAlternativeTimesAsync_WhenServiceNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _serviceRepoMock
            .Setup(r => r.GetByIdAsync(ServiceId, default))
            .ReturnsAsync((Service?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.GetAlternativeTimesAsync(ProfessionalId, ServiceId, DateTime.UtcNow.AddHours(2)));
    }

    // ── UpdateAsync: cenários de sucesso ──────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_WithNoConflict_UpdatesSchedule()
    {
        // Arrange
        var scheduleId = Guid.NewGuid();
        var existingSchedule = new Schedule
        {
            Id             = scheduleId,
            ProfessionalId = ProfessionalId,
            ServiceId      = ServiceId,
            StartDateTime  = new DateTime(2026, 6, 10, 9, 0, 0, DateTimeKind.Utc),
            EndDateTime    = new DateTime(2026, 6, 10, 10, 0, 0, DateTimeKind.Utc),
            CustomerId     = CustomerId
        };

        var newStart = new DateTime(2026, 6, 10, 14, 0, 0, DateTimeKind.Utc);

        _scheduleRepoMock
            .Setup(r => r.GetByIdAsync(scheduleId, default))
            .ReturnsAsync(existingSchedule);

        SetupServiceExists();

        _scheduleRepoMock
            .Setup(r => r.GetConflictingAsync(ProfessionalId, newStart, newStart.AddMinutes(60), default))
            .ReturnsAsync([]);

        _scheduleRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Schedule>(), default))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.UpdateAsync(scheduleId, newStart, "Notas atualizadas");

        // Assert
        Assert.Equal(newStart,                    result.StartDateTime);
        Assert.Equal(newStart.AddMinutes(60),     result.EndDateTime);
        Assert.Equal("Notas atualizadas",         result.Notes);
        
        _syncQueueMock.Verify(q => q.EnqueueAsync(
            It.Is<CalendarSyncMessage>(m => m.Operation == CalendarSyncOperation.Upsert), 
            default), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ShouldNotConflictWithItself()
    {
        // Arrange — atualizar um agendamento mantendo o mesmo horário não deve lançar conflito
        var scheduleId = Guid.NewGuid();
        var start = new DateTime(2026, 6, 10, 9, 0, 0, DateTimeKind.Utc);
        var existingSchedule = new Schedule
        {
            Id             = scheduleId,
            ProfessionalId = ProfessionalId,
            ServiceId      = ServiceId,
            StartDateTime  = start,
            EndDateTime    = start.AddMinutes(60),
            CustomerId     = CustomerId
        };

        _scheduleRepoMock
            .Setup(r => r.GetByIdAsync(scheduleId, default))
            .ReturnsAsync(existingSchedule);

        SetupServiceExists();

        // GetConflictingAsync retorna o próprio agendamento (normal no banco)
        _scheduleRepoMock
            .Setup(r => r.GetConflictingAsync(ProfessionalId, start, start.AddMinutes(60), default))
            .ReturnsAsync([existingSchedule]);

        _scheduleRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Schedule>(), default))
            .Returns(Task.CompletedTask);

        // Act — não deve lançar exceção
        var result = await _sut.UpdateAsync(scheduleId, start, null);

        Assert.Equal(scheduleId, result.Id);
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _scheduleRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((Schedule?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.UpdateAsync(Guid.NewGuid(), DateTime.UtcNow, null));
    }

    // ── UpdateStatusAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateStatusAsync_CallsRepository()
    {
        // Arrange
        var id = Guid.NewGuid();
        _scheduleRepoMock
            .Setup(r => r.UpdateStatusAsync(id, ScheduleStatus.Confirmed, default))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.UpdateStatusAsync(id, ScheduleStatus.Confirmed);

        // Assert
        Assert.True(result);
        _scheduleRepoMock.Verify(r => r.UpdateStatusAsync(id, ScheduleStatus.Confirmed, default), Times.Once);
    }

    // ── DeleteAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_WhenScheduleHasGoogleEventId_EnqueuesDeleteMessage()
    {
        // Arrange
        var id = Guid.NewGuid();
        var schedule = new Schedule { Id = id, GoogleCalendarEventId = "google-id-123" };
        
        _scheduleRepoMock.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync(schedule);
        _scheduleRepoMock.Setup(r => r.DeleteAsync(id, default)).ReturnsAsync(true);

        // Act
        var result = await _sut.DeleteAsync(id);

        // Assert
        Assert.True(result);
        _syncQueueMock.Verify(q => q.EnqueueAsync(
            It.Is<CalendarSyncMessage>(m => 
                m.Operation == CalendarSyncOperation.Delete && 
                m.GoogleCalendarEventId == "google-id-123"), 
            default), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenScheduleExists_TriggersWaitlistProcessing()
    {
        // Arrange
        var id    = Guid.NewGuid();
        var start = new DateTime(2026, 6, 10, 10, 0, 0, DateTimeKind.Utc);
        var schedule = new Schedule
        {
            Id             = id,
            ProfessionalId = ProfessionalId,
            StartDateTime  = start,
            EndDateTime    = start.AddHours(1)
        };

        _scheduleRepoMock.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync(schedule);
        _scheduleRepoMock.Setup(r => r.DeleteAsync(id, default)).ReturnsAsync(true);

        // Act
        await _sut.DeleteAsync(id);

        // Assert — deve acionar a lista de espera com os dados do slot liberado
        _waitlistSvcMock.Verify(w => w.ProcessCancellationAsync(
            It.IsAny<Guid>(), ProfessionalId, start, start.AddHours(1), default), Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenStatusIsCancelled_TriggersWaitlistProcessing()
    {
        // Arrange
        var id    = Guid.NewGuid();
        var start = new DateTime(2026, 6, 10, 10, 0, 0, DateTimeKind.Utc);
        var schedule = new Schedule
        {
            Id             = id,
            ProfessionalId = ProfessionalId,
            StartDateTime  = start,
            EndDateTime    = start.AddHours(1)
        };

        _scheduleRepoMock.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync(schedule);
        _scheduleRepoMock.Setup(r => r.UpdateStatusAsync(id, ScheduleStatus.Cancelled, default)).ReturnsAsync(true);

        // Act
        await _sut.UpdateStatusAsync(id, ScheduleStatus.Cancelled);

        // Assert — lista de espera deve ter sido acionada
        _waitlistSvcMock.Verify(w => w.ProcessCancellationAsync(
            It.IsAny<Guid>(), ProfessionalId, start, start.AddHours(1), default), Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenStatusIsNotCancelled_DoesNotTriggerWaitlist()
    {
        // Arrange
        var id = Guid.NewGuid();
        _scheduleRepoMock
            .Setup(r => r.UpdateStatusAsync(id, ScheduleStatus.Confirmed, default))
            .ReturnsAsync(true);

        // Act
        await _sut.UpdateStatusAsync(id, ScheduleStatus.Confirmed);

        // Assert — nenhuma chamada à lista de espera para status não-cancelamento
        _waitlistSvcMock.Verify(w => w.ProcessCancellationAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), default), Times.Never);
    }

    // ── Blockouts (Folgas) ─────────────────────────────────────────────────────

    [Fact]
    public async Task CreateBlockoutAsync_WithValidData_ReturnsBlockoutAndEnqueuesSync()
    {
        // Arrange
        var start = new DateTime(2026, 6, 10, 8, 0, 0, DateTimeKind.Utc);
        var end   = new DateTime(2026, 6, 10, 18, 0, 0, DateTimeKind.Utc);
        var reason = "Feriado Local";

        _scheduleRepoMock
            .Setup(r => r.GetConflictingAsync(ProfessionalId, start, end, default))
            .ReturnsAsync(new List<Schedule>());

        _scheduleRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<Schedule>(), default))
            .ReturnsAsync((Schedule s, CancellationToken ct) => s);

        // Act
        var result = await _sut.CreateBlockoutAsync(ProfessionalId, start, end, reason, isAllDay: true);

        // Assert
        Assert.True(result.IsBlocked);
        Assert.Equal(reason, result.BlockReason);
        Assert.True(result.IsAllDay);
        Assert.Equal(ScheduleStatus.Confirmed, result.Status);

        _syncQueueMock.Verify(q => q.EnqueueAsync(
            It.Is<CalendarSyncMessage>(m => m.Operation == CalendarSyncOperation.Upsert), 
            default), Times.Once);
    }

    [Fact]
    public async Task CreateBlockoutAsync_WhenConflictExists_ThrowsInvalidOperationException()
    {
        // Arrange
        var start = new DateTime(2026, 6, 10, 8, 0, 0, DateTimeKind.Utc);
        var end   = new DateTime(2026, 6, 10, 18, 0, 0, DateTimeKind.Utc);

        var conflictingSchedule = new Schedule
        {
            Id = Guid.NewGuid(),
            StartDateTime = start.AddHours(1),
            EndDateTime = start.AddHours(2)
        };

        _scheduleRepoMock
            .Setup(r => r.GetConflictingAsync(ProfessionalId, start, end, default))
            .ReturnsAsync(new List<Schedule> { conflictingSchedule });

        // Act
        var exception = await Record.ExceptionAsync(() =>
            _sut.CreateBlockoutAsync(ProfessionalId, start, end, "Férias", false));

        // Assert
        Assert.IsType<InvalidOperationException>(exception);
        Assert.Contains("já existem agendamentos ou folgas", exception.Message);
    }

    [Fact]
    public async Task UpdateBlockoutAsync_WithValidData_UpdatesBlockoutAndEnqueuesSync()
    {
        // Arrange
        var id    = Guid.NewGuid();
        var start = new DateTime(2026, 6, 10, 8, 0, 0, DateTimeKind.Utc);
        var end   = new DateTime(2026, 6, 10, 18, 0, 0, DateTimeKind.Utc);

        var blockout = new Schedule
        {
            Id             = id,
            ProfessionalId = ProfessionalId,
            IsBlocked      = true,
            BlockReason    = "Férias"
        };

        _scheduleRepoMock.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync(blockout);
        
        _scheduleRepoMock
            .Setup(r => r.GetConflictingAsync(ProfessionalId, start, end, default))
            .ReturnsAsync(new List<Schedule> { blockout }); // Próprio blockout na query de conflito

        // Act
        var result = await _sut.UpdateBlockoutAsync(id, start, end, "Férias Editadas", isAllDay: true);

        // Assert
        Assert.Equal("Férias Editadas", result.BlockReason);
        Assert.True(result.IsAllDay);
        _scheduleRepoMock.Verify(r => r.UpdateAsync(blockout, default), Times.Once);
        _syncQueueMock.Verify(q => q.EnqueueAsync(It.IsAny<CalendarSyncMessage>(), default), Times.Once);
    }

    [Fact]
    public async Task UpdateBlockoutAsync_WhenTargetIsNotBlockout_ThrowsInvalidOperationException()
    {
        // Arrange
        var id = Guid.NewGuid();
        var schedule = new Schedule { Id = id, IsBlocked = false }; // Agendamento normal

        _scheduleRepoMock.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync(schedule);

        // Act
        var exception = await Record.ExceptionAsync(() =>
            _sut.UpdateBlockoutAsync(id, DateTime.UtcNow, DateTime.UtcNow.AddHours(1), "Erro", false));

        // Assert
        Assert.IsType<InvalidOperationException>(exception);
        Assert.Contains("não é um bloqueio", exception.Message);
    }

    // ── B26 — GetAvailableSlotsAsync ──────────────────────────────────────────

    [Fact]
    public async Task GetAvailableSlotsAsync_WhenServiceNotFound_ThrowsKeyNotFoundException()
    {
        _serviceRepoMock.Setup(r => r.GetByIdAsync(ServiceId, default)).ReturnsAsync((Service?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.GetAvailableSlotsAsync(ProfessionalId, ServiceId, new DateOnly(2099, 6, 15)));
    }

    [Fact]
    public async Task GetAvailableSlotsAsync_WhenAllSlotsAreFree_ReturnsAllDayCandidates()
    {
        // 60-min service, granularity = service duration (60 min), fallback 08:00-18:00
        // Slots: 08:00, 09:00, ..., 17:00 = 10 candidates (last ends at 18:00)
        SetupServiceExists(); // DefaultService: DurationMinutes = 60
        SetupNoConflicts();

        var date   = new DateOnly(2099, 6, 15); // data futura — sem filtro de passado
        var result = await _sut.GetAvailableSlotsAsync(ProfessionalId, ServiceId, date);

        Assert.Equal(10, result.Count);
        Assert.Equal(new DateTime(2099, 6, 15, 8, 0, 0, DateTimeKind.Utc),  result[0]);
        Assert.Equal(new DateTime(2099, 6, 15, 17, 0, 0, DateTimeKind.Utc), result[^1]);
        Assert.All(result, dt => Assert.Equal(DateTimeKind.Utc, dt.Kind));
    }

    [Fact]
    public async Task GetAvailableSlotsAsync_WhenAllSlotsConflict_ReturnsEmptyList()
    {
        SetupServiceExists();
        _scheduleRepoMock
            .Setup(r => r.GetConflictingAsync(ProfessionalId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), default))
            .ReturnsAsync([new Schedule { Id = Guid.NewGuid() }]);

        var result = await _sut.GetAvailableSlotsAsync(ProfessionalId, ServiceId, new DateOnly(2099, 6, 15));

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAvailableSlotsAsync_WhenSomeSlotsConflict_ReturnsOnlyFreeSlots()
    {
        SetupServiceExists(); // 60 min, granularity = 60 min

        var slot10h = new DateTime(2099, 6, 15, 10, 0, 0, DateTimeKind.Utc);

        // 10:00 bloqueado, restante livre
        _scheduleRepoMock
            .Setup(r => r.GetConflictingAsync(ProfessionalId,
                It.Is<DateTime>(d => d == slot10h),
                It.IsAny<DateTime>(), default))
            .ReturnsAsync([new Schedule { Id = Guid.NewGuid() }]);

        _scheduleRepoMock
            .Setup(r => r.GetConflictingAsync(ProfessionalId,
                It.Is<DateTime>(d => d != slot10h),
                It.IsAny<DateTime>(), default))
            .ReturnsAsync([]);

        var result = await _sut.GetAvailableSlotsAsync(ProfessionalId, ServiceId, new DateOnly(2099, 6, 15));

        Assert.Equal(9, result.Count); // 10 total - 1 bloqueado
        Assert.DoesNotContain(slot10h, result);
    }

    [Fact]
    public async Task GetAvailableSlotsAsync_WhenDateIsInPast_ReturnsEmptyList()
    {
        SetupServiceExists();
        SetupNoConflicts();

        var pastDate = new DateOnly(2000, 1, 1);
        var result   = await _sut.GetAvailableSlotsAsync(ProfessionalId, ServiceId, pastDate);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAvailableSlotsAsync_WhenWorkingHoursConfigured_UsesTenantHours()
    {
        // 2099-06-15 — determine day of week dynamically to avoid hardcoding
        // Configure tenant working hours: that day 09:00-22:00
        // 60-min service → slots 09:00, 10:00, ..., 21:00 = 13 candidates
        SetupServiceExists(); // DurationMinutes = 60
        SetupNoConflicts();

        var date      = new DateOnly(2099, 6, 15);
        var dayOfWeek = (int)new DateTime(2099, 6, 15).DayOfWeek;
        var settings  = new TenantSettings
        {
            WorkingHoursJson = $"[{{\"dayOfWeek\":{dayOfWeek},\"openTime\":\"09:00\",\"closeTime\":\"22:00\"}}]",
            TimeZoneId = "UTC"
        };
        _settingsRepoMock.Setup(r => r.GetAsync(default)).ReturnsAsync(settings);

        var result = await _sut.GetAvailableSlotsAsync(ProfessionalId, ServiceId, date);

        Assert.Equal(13, result.Count); // 09:00..21:00 com step 60 min
        Assert.Equal(new DateTime(2099, 6, 15, 9,  0, 0, DateTimeKind.Utc), result[0]);
        Assert.Equal(new DateTime(2099, 6, 15, 21, 0, 0, DateTimeKind.Utc), result[^1]);
    }

    [Fact]
    public async Task GetAvailableSlotsAsync_WhenDateIsDayOff_ReturnsEmpty()
    {
        SetupServiceExists();
        SetupNoConflicts();

        var settings = new TenantSettings { DaysOffJson = "[\"2099-06-15\"]" };
        _settingsRepoMock.Setup(r => r.GetAsync(default)).ReturnsAsync(settings);

        var result = await _sut.GetAvailableSlotsAsync(ProfessionalId, ServiceId, new DateOnly(2099, 6, 15));

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAvailableSlotsAsync_WhenDayNotInWorkSchedule_ReturnsEmpty()
    {
        // Configure only the day after 2099-06-15 as a working day → queried date is closed
        SetupServiceExists();
        SetupNoConflicts();

        var date          = new DateOnly(2099, 6, 15);
        var otherDayOfWeek = ((int)new DateTime(2099, 6, 15).DayOfWeek + 1) % 7;
        var settings = new TenantSettings
        {
            WorkingHoursJson = $"[{{\"dayOfWeek\":{otherDayOfWeek},\"openTime\":\"09:00\",\"closeTime\":\"18:00\"}}]"
        };
        _settingsRepoMock.Setup(r => r.GetAsync(default)).ReturnsAsync(settings);

        var result = await _sut.GetAvailableSlotsAsync(ProfessionalId, ServiceId, date);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAvailableSlotsAsync_GranularityMatchesServiceDuration()
    {
        // 20-min service, fallback 08:00-18:00
        // step = 20 min; lastPossibleStart = 17:40 → slots: 08:00..17:40 = 30 candidates
        var service20 = new Service { Id = ServiceId, Name = "Sobrancelha", DurationMinutes = 20, Price = 30m, TenantId = Guid.NewGuid() };
        _serviceRepoMock.Setup(r => r.GetByIdAsync(ServiceId, default)).ReturnsAsync(service20);
        SetupNoConflicts();

        var result = await _sut.GetAvailableSlotsAsync(ProfessionalId, ServiceId, new DateOnly(2099, 6, 15));

        Assert.Equal(30, result.Count);
        Assert.Equal(new DateTime(2099, 6, 15, 8,  0,  0, DateTimeKind.Utc), result[0]);
        Assert.Equal(new DateTime(2099, 6, 15, 17, 40, 0, DateTimeKind.Utc), result[^1]);
    }
}
