using System.Text;
using System.Text.Json;
using AgendaInteligente.Api.Contracts.Reminders;
using AgendaInteligente.Api.Domain.Entities;
using AgendaInteligente.Api.Domain.Enums;
using AgendaInteligente.Api.Repositories.Interfaces;
using AgendaInteligente.Api.Services;
using AgendaInteligente.Api.Services.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AgendaInteligente.Api.Tests.Services;

public sealed class ReminderServiceTests
{
    // ── Fixtures ──────────────────────────────────────────────────────────────────
    private readonly Mock<ITenantSettingsRepository> _settingsRepoMock = new();
    private readonly Mock<IScheduleRepository>       _scheduleRepoMock = new();
    private readonly Mock<IWhatsAppSendService>       _sendServiceMock  = new();
    private readonly Mock<IDistributedCache>          _cacheMock        = new();

    private static readonly Guid TenantId     = Guid.NewGuid();
    private static readonly Guid ScheduleId   = Guid.NewGuid();
    private static readonly Guid CustomerId   = Guid.NewGuid();
    private static readonly Guid ServiceId    = Guid.NewGuid();
    private static readonly Guid ProfId       = Guid.NewGuid();
    private const string         Phone        = "5511999990000";

    // ── Helpers ────────────────────────────────────────────────────────────────

    private ReminderService BuildSut(DateTimeOffset utcNow)
    {
        var fakeTime = new FixedTimeProvider(utcNow);
        return new ReminderService(
            _settingsRepoMock.Object,
            _scheduleRepoMock.Object,
            _sendServiceMock.Object,
            _cacheMock.Object,
            NullLogger<ReminderService>.Instance,
            fakeTime);
    }

    private static TenantSettings SettingsWithReminder(int leadHours = 24) => new()
    {
        TenantId              = TenantId,
        ReminderLeadTimeHours = leadHours
    };

    private static Schedule MakeSchedule(DateTime start) => new()
    {
        Id             = ScheduleId,
        TenantId       = TenantId,
        ProfessionalId = ProfId,
        StartDateTime  = start,
        EndDateTime    = start.AddHours(1),
        Status         = ScheduleStatus.Pending,
        Customer       = new Customer { Id = CustomerId, Name = "Joao", PhoneNumber = Phone, TenantId = TenantId },
        Service        = new Service  { Id = ServiceId,  Name = "Corte", DurationMinutes = 60, Price = 50m, TenantId = TenantId },
        Professional   = new Professional { Id = ProfId, Name = "Carlos", Email = "carlos@test.com", PasswordHash = "hash", TenantId = TenantId }
    };

    // ── Horário de silêncio ───────────────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(6)]
    [InlineData(22)]
    [InlineData(23)]
    public async Task ProcessRemindersAsync_DuringQuietHours_DoesNotQueryRepository(int hour)
    {
        var sut = BuildSut(new DateTimeOffset(2099, 6, 15, hour, 0, 0, TimeSpan.Zero));

        await sut.ProcessRemindersAsync();

        _settingsRepoMock.Verify(r => r.GetAllWithReminderEnabledAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Sem tenants configurados ──────────────────────────────────────────────────

    [Fact]
    public async Task ProcessRemindersAsync_WhenNoTenantsEnabled_DoesNotSend()
    {
        var sut = BuildSut(new DateTimeOffset(2099, 6, 15, 10, 0, 0, TimeSpan.Zero));

        _settingsRepoMock
            .Setup(r => r.GetAllWithReminderEnabledAsync(default))
            .ReturnsAsync([]);

        await sut.ProcessRemindersAsync();

        _sendServiceMock.Verify(s => s.SendTextMessageAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Deduplicação ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task ProcessRemindersAsync_WhenAlreadySent_SkipsSend()
    {
        var sut      = BuildSut(new DateTimeOffset(2099, 6, 14, 10, 0, 0, TimeSpan.Zero)); // 10h UTC
        var apptTime = new DateTime(2099, 6, 15, 10, 0, 0, DateTimeKind.Utc); // amanhã às 10h (leadTime=24h)

        _settingsRepoMock.Setup(r => r.GetAllWithReminderEnabledAsync(default))
            .ReturnsAsync([SettingsWithReminder(24)]);

        _scheduleRepoMock.Setup(r => r.GetUpcomingForReminderAsync(TenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), default))
            .ReturnsAsync([MakeSchedule(apptTime)]);

        // Simula chave já existente no Redis
        _cacheMock.Setup(c => c.GetAsync($"reminder:sent:{ScheduleId}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes("1"));

        await sut.ProcessRemindersAsync();

        _sendServiceMock.Verify(s => s.SendTextMessageAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Customer sem telefone ─────────────────────────────────────────────────────

    [Fact]
    public async Task ProcessRemindersAsync_WhenCustomerHasNoPhone_SkipsSend()
    {
        var sut      = BuildSut(new DateTimeOffset(2099, 6, 14, 10, 0, 0, TimeSpan.Zero));
        var apptTime = new DateTime(2099, 6, 15, 10, 0, 0, DateTimeKind.Utc);

        var scheduleNoPhone = MakeSchedule(apptTime);
        scheduleNoPhone.Customer!.PhoneNumber = null!;

        _settingsRepoMock.Setup(r => r.GetAllWithReminderEnabledAsync(default))
            .ReturnsAsync([SettingsWithReminder(24)]);
        _scheduleRepoMock.Setup(r => r.GetUpcomingForReminderAsync(TenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), default))
            .ReturnsAsync([scheduleNoPhone]);

        await sut.ProcessRemindersAsync();

        _sendServiceMock.Verify(s => s.SendTextMessageAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Envio bem-sucedido ────────────────────────────────────────────────────────

    [Fact]
    public async Task ProcessRemindersAsync_WhenSendSucceeds_SetsSentKeyInRedis()
    {
        var sut      = BuildSut(new DateTimeOffset(2099, 6, 14, 10, 0, 0, TimeSpan.Zero));
        var apptTime = new DateTime(2099, 6, 15, 10, 0, 0, DateTimeKind.Utc);

        _settingsRepoMock.Setup(r => r.GetAllWithReminderEnabledAsync(default))
            .ReturnsAsync([SettingsWithReminder(24)]);
        _scheduleRepoMock.Setup(r => r.GetUpcomingForReminderAsync(TenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), default))
            .ReturnsAsync([MakeSchedule(apptTime)]);
        _cacheMock.Setup(c => c.GetAsync($"reminder:sent:{ScheduleId}", It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);
        _sendServiceMock.Setup(s => s.SendTextMessageAsync(TenantId, Phone, It.IsAny<string>(), default))
            .ReturnsAsync(true);

        await sut.ProcessRemindersAsync();

        _cacheMock.Verify(c => c.SetAsync(
            $"reminder:sent:{ScheduleId}",
            It.IsAny<byte[]>(),
            It.Is<DistributedCacheEntryOptions>(o => o.AbsoluteExpirationRelativeToNow == TimeSpan.FromHours(48)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessRemindersAsync_WhenSendSucceeds_SetsPendingConfirmKeyInRedis()
    {
        var sut      = BuildSut(new DateTimeOffset(2099, 6, 14, 10, 0, 0, TimeSpan.Zero));
        var apptTime = new DateTime(2099, 6, 15, 10, 0, 0, DateTimeKind.Utc);

        _settingsRepoMock.Setup(r => r.GetAllWithReminderEnabledAsync(default))
            .ReturnsAsync([SettingsWithReminder(24)]);
        _scheduleRepoMock.Setup(r => r.GetUpcomingForReminderAsync(TenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), default))
            .ReturnsAsync([MakeSchedule(apptTime)]);
        _cacheMock.Setup(c => c.GetAsync($"reminder:sent:{ScheduleId}", It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);
        _sendServiceMock.Setup(s => s.SendTextMessageAsync(TenantId, Phone, It.IsAny<string>(), default))
            .ReturnsAsync(true);

        await sut.ProcessRemindersAsync();

        _cacheMock.Verify(c => c.SetAsync(
            $"reminder:confirm:{TenantId}:{Phone}",
            It.IsAny<byte[]>(),
            It.Is<DistributedCacheEntryOptions>(o => o.AbsoluteExpirationRelativeToNow == TimeSpan.FromHours(4)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessRemindersAsync_WhenSendFails_DoesNotSetRedisKeys()
    {
        var sut      = BuildSut(new DateTimeOffset(2099, 6, 14, 10, 0, 0, TimeSpan.Zero));
        var apptTime = new DateTime(2099, 6, 15, 10, 0, 0, DateTimeKind.Utc);

        _settingsRepoMock.Setup(r => r.GetAllWithReminderEnabledAsync(default))
            .ReturnsAsync([SettingsWithReminder(24)]);
        _scheduleRepoMock.Setup(r => r.GetUpcomingForReminderAsync(TenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), default))
            .ReturnsAsync([MakeSchedule(apptTime)]);
        _cacheMock.Setup(c => c.GetAsync($"reminder:sent:{ScheduleId}", It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);
        _sendServiceMock.Setup(s => s.SendTextMessageAsync(TenantId, Phone, It.IsAny<string>(), default))
            .ReturnsAsync(false);

        await sut.ProcessRemindersAsync();

        _cacheMock.Verify(c => c.SetAsync(
            It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Mensagem gerada ───────────────────────────────────────────────────────────

    [Fact]
    public void BuildMessage_ContainsAllFields()
    {
        var start   = new DateTime(2099, 6, 15, 14, 30, 0, DateTimeKind.Utc);
        var message = ReminderService.BuildMessage("Joao", "Corte", "Carlos", start);

        Assert.Contains("Joao",       message);
        Assert.Contains("Corte",      message);
        Assert.Contains("Carlos",     message);
        Assert.Contains("15/06/2099", message);
        Assert.Contains("14:30",      message);
        Assert.Contains("1 - Confirmar", message);
        Assert.Contains("2 - Remarcar",  message);
        Assert.Contains("3 - Cancelar",  message);
    }

    // ── Janela de tempo ───────────────────────────────────────────────────────────

    [Fact]
    public async Task ProcessRemindersAsync_QueriesCorrectWindow_ForLeadTime()
    {
        // 10h UTC agora, leadTime=24h → janela [09:30, 10:30] de amanhã
        var sut = BuildSut(new DateTimeOffset(2099, 6, 14, 10, 0, 0, TimeSpan.Zero));
        var expectedFrom = new DateTime(2099, 6, 15, 9, 30, 0, DateTimeKind.Utc);
        var expectedTo   = new DateTime(2099, 6, 15, 10, 30, 0, DateTimeKind.Utc);

        _settingsRepoMock.Setup(r => r.GetAllWithReminderEnabledAsync(default))
            .ReturnsAsync([SettingsWithReminder(24)]);
        _scheduleRepoMock.Setup(r => r.GetUpcomingForReminderAsync(TenantId, expectedFrom, expectedTo, default))
            .ReturnsAsync([]);

        await sut.ProcessRemindersAsync();

        _scheduleRepoMock.Verify(r => r.GetUpcomingForReminderAsync(TenantId, expectedFrom, expectedTo, default), Times.Once);
    }
}

// ── Auxiliar de testes: TimeProvider com hora fixa ────────────────────────────

internal sealed class FixedTimeProvider : TimeProvider
{
    private readonly DateTimeOffset _utcNow;
    public FixedTimeProvider(DateTimeOffset utcNow) => _utcNow = utcNow;
    public override DateTimeOffset GetUtcNow() => _utcNow;
}
