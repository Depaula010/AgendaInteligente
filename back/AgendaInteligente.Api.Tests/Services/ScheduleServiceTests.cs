using AgendaInteligente.Api.Domain.Entities;
using AgendaInteligente.Api.Domain.Enums;
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
    private readonly Mock<ICalendarSyncQueue>        _syncQueueMock    = new();
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
        _sut = new ScheduleService(
            _scheduleRepoMock.Object,
            _serviceRepoMock.Object,
            _syncQueueMock.Object,
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

    // ── CreateAsync: validação de conflito ─────────────────────────────────────

    [Fact]
    public async Task CreateAsync_WhenConflictExists_ThrowsInvalidOperationException()
    {
        // Arrange
        var start = new DateTime(2026, 6, 10, 9, 0, 0, DateTimeKind.Utc);
        var conflicting = new Schedule
        {
            Id             = Guid.NewGuid(),
            ProfessionalId = ProfessionalId,
            StartDateTime  = start,
            EndDateTime    = start.AddMinutes(60)
        };

        SetupServiceExists();
        SetupConflicts([conflicting]);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.CreateAsync(CustomerId, ProfessionalId, ServiceId, start));
    }

    [Fact]
    public async Task CreateAsync_WhenConflictExists_DoesNotCallRepositoryCreate()
    {
        // Arrange
        var start = new DateTime(2026, 6, 10, 9, 0, 0, DateTimeKind.Utc);
        SetupServiceExists();
        SetupConflicts([new Schedule { Id = Guid.NewGuid() }]);

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(
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
    public async Task CreateAsync_OverlappingInterval_ThrowsConflict(DateTime existStart, DateTime existEnd)
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
        SetupConflicts([existing]);

        await Assert.ThrowsAsync<InvalidOperationException>(
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
}
