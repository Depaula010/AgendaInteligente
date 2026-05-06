using AgendaInteligente.Api.Domain.Entities;
using AgendaInteligente.Api.Repositories.Interfaces;
using AgendaInteligente.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AgendaInteligente.Api.Tests.Services;

public sealed class ProfessionalServiceTests
{
    private readonly Mock<IProfessionalRepository> _repoMock = new();
    private readonly ProfessionalService _sut;

    public ProfessionalServiceTests()
        => _sut = new ProfessionalService(_repoMock.Object, NullLogger<ProfessionalService>.Instance);

    // ── CreateAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_WithValidData_ReturnsProfessional()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByEmailAsync("joao@barber.com", default))
                 .ReturnsAsync((Professional?)null);

        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Professional>(), default))
                 .ReturnsAsync((Professional p, CancellationToken _) => p);

        // Act
        var result = await _sut.CreateAsync("João", "joao@barber.com", "hash_bcrypt");

        // Assert
        Assert.Equal("João",           result.Name);
        Assert.Equal("joao@barber.com", result.Email);
        Assert.Equal("hash_bcrypt",    result.PasswordHash);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateEmail_ThrowsInvalidOperationException()
    {
        // Arrange — e-mail já cadastrado neste Tenant
        var existingProfessional = new Professional
        {
            Name         = "Outro",
            Email        = "joao@barber.com",
            PasswordHash = "hash"
        };

        _repoMock.Setup(r => r.GetByEmailAsync("joao@barber.com", default))
                 .ReturnsAsync(existingProfessional);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.CreateAsync("João", "joao@barber.com", "hash_bcrypt"));
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateEmail_DoesNotCallRepositoryCreate()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), default))
                 .ReturnsAsync(new Professional { Name = "X", Email = "x@x.com", PasswordHash = "h" });

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.CreateAsync("Novo", "x@x.com", "hash"));

        // Assert — nenhum insert deve ter sido feito
        _repoMock.Verify(r => r.CreateAsync(It.IsAny<Professional>(), default), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithCalendarColor_SetsColor()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), default))
                 .ReturnsAsync((Professional?)null);

        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Professional>(), default))
                 .ReturnsAsync((Professional p, CancellationToken _) => p);

        // Act
        var result = await _sut.CreateAsync("Maria", "maria@studio.com", "hash", "#FF5733");

        // Assert
        Assert.Equal("#FF5733", result.CalendarColor);
    }

    // ── UpdateAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_WithValidId_UpdatesFields()
    {
        // Arrange
        var id = Guid.NewGuid();
        var existing = new Professional { Name = "João", Email = "j@b.com", PasswordHash = "h" };

        _repoMock.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Professional>(), default)).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.UpdateAsync(id, "João Silva", "#4285F4", true);

        // Assert
        Assert.Equal("João Silva", result.Name);
        Assert.Equal("#4285F4",    result.CalendarColor);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidId_ThrowsKeyNotFoundException()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default))
                 .ReturnsAsync((Professional?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.UpdateAsync(Guid.NewGuid(), "Qualquer", null, true));
    }

    // ── DeleteAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ReturnsTrue_WhenFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.DeleteAsync(id, default)).ReturnsAsync(true);

        // Act
        var result = await _sut.DeleteAsync(id);

        // Assert
        Assert.True(result);
        _repoMock.Verify(r => r.DeleteAsync(id, default), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenNotFound()
    {
        // Arrange
        _repoMock.Setup(r => r.DeleteAsync(It.IsAny<Guid>(), default)).ReturnsAsync(false);

        // Act
        var result = await _sut.DeleteAsync(Guid.NewGuid());

        // Assert
        Assert.False(result);
    }

    // ── GetAllActiveAsync / GetByIdAsync ───────────────────────────────────────

    [Fact]
    public async Task GetAllActiveAsync_DelegatesToRepository()
    {
        // Arrange
        var list = (IReadOnlyList<Professional>)[
            new Professional { Name = "A", Email = "a@a.com", PasswordHash = "h" },
            new Professional { Name = "B", Email = "b@b.com", PasswordHash = "h" }
        ];
        _repoMock.Setup(r => r.GetAllActiveAsync(default)).ReturnsAsync(list);

        // Act
        var result = await _sut.GetAllActiveAsync();

        // Assert
        Assert.Equal(2, result.Count);
    }
}
