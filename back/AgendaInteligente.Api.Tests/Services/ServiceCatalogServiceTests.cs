using AgendaInteligente.Api.Domain.Entities;
using AgendaInteligente.Api.Repositories.Interfaces;
using AgendaInteligente.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AgendaInteligente.Api.Tests.Services;

public sealed class ServiceCatalogServiceTests
{
    private readonly Mock<IServiceCatalogRepository> _repoMock = new();
    private readonly ServiceCatalogService _sut;

    public ServiceCatalogServiceTests()
        => _sut = new ServiceCatalogService(_repoMock.Object, NullLogger<ServiceCatalogService>.Instance);

    // ── CreateAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_WithValidData_ReturnsService()
    {
        // Arrange
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Service>(), default))
                 .ReturnsAsync((Service s, CancellationToken _) => s);

        // Act
        var result = await _sut.CreateAsync("Corte", 30, 40m, "Corte simples");

        // Assert
        Assert.Equal("Corte",    result.Name);
        Assert.Equal(30,         result.DurationMinutes);
        Assert.Equal(40m,        result.Price);
        Assert.Equal("Corte simples", result.Description);
        Assert.True(result.IsActive);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-30)]
    public async Task CreateAsync_WithInvalidDuration_ThrowsArgumentException(int duration)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.CreateAsync("Serviço", duration, 50m));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-0.01)]
    public async Task CreateAsync_WithNegativePrice_ThrowsArgumentException(decimal price)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.CreateAsync("Serviço", 30, price));
    }

    [Fact]
    public async Task CreateAsync_WithZeroPrice_Succeeds()
    {
        // Arrange — preço zero é válido (ex: serviço gratuito/promoção)
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Service>(), default))
                 .ReturnsAsync((Service s, CancellationToken _) => s);

        // Act
        var result = await _sut.CreateAsync("Consulta", 20, 0m);

        // Assert
        Assert.Equal(0m, result.Price);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidData_DoesNotCallRepository()
    {
        // Act
        await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.CreateAsync("Serviço", 0, 50m));

        // Assert — repositório não deve ser chamado
        _repoMock.Verify(r => r.CreateAsync(It.IsAny<Service>(), default), Times.Never);
    }

    // ── UpdateAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_WithValidData_UpdatesAllFields()
    {
        // Arrange
        var id = Guid.NewGuid();
        var existing = new Service { Name = "Corte", DurationMinutes = 30, Price = 40m };

        _repoMock.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Service>(), default)).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.UpdateAsync(id, "Corte Premium", 45, 60m, "Premium", "#BADA55", true);

        // Assert
        Assert.Equal("Corte Premium", result.Name);
        Assert.Equal(45,              result.DurationMinutes);
        Assert.Equal(60m,             result.Price);
        Assert.Equal("Premium",       result.Description);
        Assert.Equal("#BADA55",       result.CalendarColor);
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default))
                 .ReturnsAsync((Service?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.UpdateAsync(Guid.NewGuid(), "X", 30, 10m, null, null, true));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task UpdateAsync_WithInvalidDuration_ThrowsArgumentException(int duration)
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.UpdateAsync(Guid.NewGuid(), "X", duration, 10m, null, null, true));
    }

    // ── DeleteAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ReturnsTrue_WhenSoftDeleteSucceeds()
    {
        // Arrange
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.DeleteAsync(id, default)).ReturnsAsync(true);

        // Act
        var result = await _sut.DeleteAsync(id);

        // Assert
        Assert.True(result);
    }

    // ── GetAllActiveAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllActiveAsync_DelegatesToRepository()
    {
        // Arrange
        var list = (IReadOnlyList<Service>)[
            new Service { Name = "Corte",     DurationMinutes = 30, Price = 40m },
            new Service { Name = "Barba",     DurationMinutes = 20, Price = 25m },
            new Service { Name = "Hidratação",DurationMinutes = 45, Price = 70m }
        ];
        _repoMock.Setup(r => r.GetAllActiveAsync(default)).ReturnsAsync(list);

        // Act
        var result = await _sut.GetAllActiveAsync();

        // Assert
        Assert.Equal(3, result.Count);
        _repoMock.Verify(r => r.GetAllActiveAsync(default), Times.Once);
    }
}
