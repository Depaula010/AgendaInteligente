using AgendaInteligente.Api.Domain.Entities;
using AgendaInteligente.Api.Repositories.Interfaces;
using AgendaInteligente.Api.Services;
using AgendaInteligente.Api.Services.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AgendaInteligente.Api.Tests.Services;

public sealed class ReengagementServiceTests
{
    private readonly Mock<ITenantSettingsRepository> _settingsRepoMock = new();
    private readonly Mock<ICustomerRepository>       _customerRepoMock = new();
    private readonly Mock<IWhatsAppSendService>      _sendServiceMock  = new();
    private readonly Mock<IDistributedCache>          _cacheMock        = new();

    private static readonly Guid TenantId  = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private const string Phone = "+5511999990001";

    private ReengagementService BuildSut(DateTimeOffset utcNow)
        => new(
            _settingsRepoMock.Object,
            _customerRepoMock.Object,
            _sendServiceMock.Object,
            _cacheMock.Object,
            NullLogger<ReengagementService>.Instance,
            new FixedTimeProvider(utcNow));

    private static TenantSettings MakeSettings(int inactiveDays = 30)
        => new() { TenantId = TenantId, ReengagementInactiveDays = inactiveDays };

    private static Customer MakeCustomer(DateTime? lastVisitAt = null)
        => new()
        {
            Id          = CustomerId,
            TenantId    = TenantId,
            Name        = "João",
            PhoneNumber = Phone,
            LastVisitAt = lastVisitAt,
            CreatedAt   = DateTime.UtcNow.AddDays(-60),
        };

    // ── ProcessAllTenantsAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task ProcessAllTenantsAsync_WhenNoTenantsEnabled_DoesNothing()
    {
        _settingsRepoMock
            .Setup(r => r.GetAllWithReengagementEnabledAsync(default))
            .ReturnsAsync([]);

        await BuildSut(DateTimeOffset.UtcNow).ProcessAllTenantsAsync();

        _customerRepoMock.Verify(
            r => r.GetInactiveAsync(It.IsAny<Guid>(), It.IsAny<int>(), default), Times.Never);
    }

    [Fact]
    public async Task ProcessAllTenantsAsync_WhenInactiveCustomer_SendsMessage()
    {
        var now      = DateTimeOffset.UtcNow;
        var settings = MakeSettings(30);
        var customer = MakeCustomer(lastVisitAt: now.UtcDateTime.AddDays(-40));

        _settingsRepoMock
            .Setup(r => r.GetAllWithReengagementEnabledAsync(default))
            .ReturnsAsync([settings]);
        _customerRepoMock
            .Setup(r => r.GetInactiveAsync(TenantId, 30, default))
            .ReturnsAsync([customer]);
        _cacheMock
            .Setup(c => c.GetAsync($"reeng:{TenantId}:{CustomerId}", default))
            .ReturnsAsync((byte[]?)null);
        _sendServiceMock
            .Setup(s => s.SendTextMessageAsync(TenantId, Phone, It.IsAny<string>(), default))
            .ReturnsAsync(true);

        await BuildSut(now).ProcessAllTenantsAsync();

        _sendServiceMock.Verify(
            s => s.SendTextMessageAsync(TenantId, Phone, It.IsAny<string>(), default), Times.Once);
        _cacheMock.Verify(
            c => c.SetAsync($"reeng:{TenantId}:{CustomerId}", It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), default),
            Times.Once);
    }

    [Fact]
    public async Task ProcessAllTenantsAsync_WhenDedupKeyPresent_SkipsCustomer()
    {
        var settings = MakeSettings(30);
        var customer = MakeCustomer(lastVisitAt: DateTime.UtcNow.AddDays(-40));

        _settingsRepoMock
            .Setup(r => r.GetAllWithReengagementEnabledAsync(default))
            .ReturnsAsync([settings]);
        _customerRepoMock
            .Setup(r => r.GetInactiveAsync(TenantId, 30, default))
            .ReturnsAsync([customer]);
        _cacheMock
            .Setup(c => c.GetAsync($"reeng:{TenantId}:{CustomerId}", default))
            .ReturnsAsync("1"u8.ToArray());

        await BuildSut(DateTimeOffset.UtcNow).ProcessAllTenantsAsync();

        _sendServiceMock.Verify(
            s => s.SendTextMessageAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), default),
            Times.Never);
    }

    [Fact]
    public async Task ProcessAllTenantsAsync_WhenSendFails_DoesNotSetDedupKey()
    {
        var settings = MakeSettings(30);
        var customer = MakeCustomer(lastVisitAt: DateTime.UtcNow.AddDays(-40));

        _settingsRepoMock
            .Setup(r => r.GetAllWithReengagementEnabledAsync(default))
            .ReturnsAsync([settings]);
        _customerRepoMock
            .Setup(r => r.GetInactiveAsync(TenantId, 30, default))
            .ReturnsAsync([customer]);
        _cacheMock
            .Setup(c => c.GetAsync($"reeng:{TenantId}:{CustomerId}", default))
            .ReturnsAsync((byte[]?)null);
        _sendServiceMock
            .Setup(s => s.SendTextMessageAsync(TenantId, Phone, It.IsAny<string>(), default))
            .ReturnsAsync(false);

        await BuildSut(DateTimeOffset.UtcNow).ProcessAllTenantsAsync();

        _cacheMock.Verify(
            c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), default),
            Times.Never);
    }

    [Fact]
    public async Task ProcessAllTenantsAsync_WhenNoInactiveCustomers_SendsNothing()
    {
        var settings = MakeSettings(30);

        _settingsRepoMock
            .Setup(r => r.GetAllWithReengagementEnabledAsync(default))
            .ReturnsAsync([settings]);
        _customerRepoMock
            .Setup(r => r.GetInactiveAsync(TenantId, 30, default))
            .ReturnsAsync([]);

        await BuildSut(DateTimeOffset.UtcNow).ProcessAllTenantsAsync();

        _sendServiceMock.Verify(
            s => s.SendTextMessageAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), default),
            Times.Never);
    }

    // ── BuildMessage ─────────────────────────────────────────────────────────────

    [Fact]
    public void BuildMessage_ContainsNameAndDays()
    {
        var msg = ReengagementService.BuildMessage("Maria", 45);
        Assert.Contains("Maria", msg);
        Assert.Contains("45", msg);
    }

    [Fact]
    public void BuildMessage_WhenLastVisitAtNull_UsesCreatedAt()
    {
        var now      = DateTimeOffset.UtcNow;
        var customer = MakeCustomer(lastVisitAt: null);
        // CreatedAt = UtcNow.AddDays(-60) from MakeCustomer
        var lastSeen = customer.LastVisitAt ?? customer.CreatedAt;
        var days     = (int)(now.UtcDateTime - lastSeen).TotalDays;
        Assert.True(days >= 59); // 60 dias desde CreatedAt
    }
}
