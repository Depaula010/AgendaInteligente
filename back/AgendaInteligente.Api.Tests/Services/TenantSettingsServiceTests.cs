using AgendaInteligente.Api.Domain.Entities;
using AgendaInteligente.Api.Repositories.Interfaces;
using AgendaInteligente.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AgendaInteligente.Api.Tests.Services;

public sealed class TenantSettingsServiceTests
{
    private readonly Mock<ITenantSettingsRepository> _repoMock = new();
    private readonly TenantSettingsService _sut;

    public TenantSettingsServiceTests()
        => _sut = new TenantSettingsService(_repoMock.Object, NullLogger<TenantSettingsService>.Instance);

    // ── GetAsync ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAsync_WhenSettingsExist_ReturnsSettings()
    {
        // Arrange
        var settings = new TenantSettings { BotDisplayName = "Barbearia do Zé" };
        _repoMock.Setup(r => r.GetAsync(default)).ReturnsAsync(settings);

        // Act
        var result = await _sut.GetAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Barbearia do Zé", result.BotDisplayName);
    }

    [Fact]
    public async Task GetAsync_WhenSettingsDoNotExist_ReturnsNull()
    {
        // Arrange
        _repoMock.Setup(r => r.GetAsync(default)).ReturnsAsync((TenantSettings?)null);

        // Act
        var result = await _sut.GetAsync();

        // Assert
        Assert.Null(result);
    }

    // ── CreateAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_WhenNoSettingsExist_CreatesSuccessfully()
    {
        // Arrange — ainda não existem configurações para este Tenant
        _repoMock.Setup(r => r.GetAsync(default)).ReturnsAsync((TenantSettings?)null);

        var newSettings = new TenantSettings
        {
            BotDisplayName         = "Studio Ana",
            ReminderLeadTimeHours  = 48,
            ReengagementInactiveDays = 15
        };

        _repoMock.Setup(r => r.CreateAsync(newSettings, default)).ReturnsAsync(newSettings);

        // Act
        var result = await _sut.CreateAsync(newSettings);

        // Assert
        Assert.Equal("Studio Ana", result.BotDisplayName);
        Assert.Equal(48,           result.ReminderLeadTimeHours);
        _repoMock.Verify(r => r.CreateAsync(newSettings, default), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenSettingsAlreadyExist_ThrowsInvalidOperationException()
    {
        // Arrange — configurações já existem (invariante 1:1)
        _repoMock.Setup(r => r.GetAsync(default))
                 .ReturnsAsync(new TenantSettings { BotDisplayName = "Existente" });

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.CreateAsync(new TenantSettings()));
    }

    [Fact]
    public async Task CreateAsync_WhenAlreadyExists_DoesNotCallRepositoryCreate()
    {
        // Arrange
        _repoMock.Setup(r => r.GetAsync(default))
                 .ReturnsAsync(new TenantSettings());

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.CreateAsync(new TenantSettings()));

        // Assert
        _repoMock.Verify(r => r.CreateAsync(It.IsAny<TenantSettings>(), default), Times.Never);
    }

    // ── UpdateAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_CallsRepositoryAndReturnsSettings()
    {
        // Arrange
        var settings = new TenantSettings
        {
            BotDisplayName        = "Atualizado",
            ReminderLeadTimeHours = 12
        };

        _repoMock.Setup(r => r.UpdateAsync(settings, default)).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.UpdateAsync(settings);

        // Assert
        Assert.Equal("Atualizado", result.BotDisplayName);
        _repoMock.Verify(r => r.UpdateAsync(settings, default), Times.Once);
    }
}
