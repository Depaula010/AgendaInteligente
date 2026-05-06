using AgendaInteligente.Api.Contracts.Requests;
using AgendaInteligente.Api.Domain.Entities;
using AgendaInteligente.Api.Repositories.Interfaces;
using AgendaInteligente.Api.Services;
using Moq;
using Xunit;

namespace AgendaInteligente.Api.Tests.Services;

public sealed class TenantServiceTests
{
    private readonly Mock<ITenantRepository> _repoMock = new();
    private readonly TenantService _sut;

    public TenantServiceTests() => _sut = new TenantService(_repoMock.Object);

    // ── Cenários de sucesso ────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_WithValidNewSlug_ReturnsSuccess()
    {
        // Arrange
        var request = new CreateTenantRequest("Barbearia do Zé", "barbearia-do-ze");

        _repoMock.Setup(r => r.SlugExistsAsync("barbearia-do-ze", default))
                 .ReturnsAsync(false);

        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Tenant>(), default))
                 .ReturnsAsync((Tenant t, CancellationToken _) => t);

        // Act
        var result = await _sut.CreateAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Barbearia do Zé", result.Value.Name);
        Assert.Equal("barbearia-do-ze", result.Value.Slug);
        Assert.True(result.Value.IsActive);
    }

    [Fact]
    public async Task CreateAsync_NormalizesSlugToLowercase()
    {
        // Arrange
        var request = new CreateTenantRequest("Clínica Alfa", " CLINICA-ALFA ");

        _repoMock.Setup(r => r.SlugExistsAsync("clinica-alfa", default))
                 .ReturnsAsync(false);

        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Tenant>(), default))
                 .ReturnsAsync((Tenant t, CancellationToken _) => t);

        // Act
        var result = await _sut.CreateAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("clinica-alfa", result.Value.Slug);
    }

    // ── Cenários de falha ──────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_WithExistingSlug_ReturnsConflictError()
    {
        // Arrange
        _repoMock.Setup(r => r.SlugExistsAsync("barbearia-do-ze", default))
                 .ReturnsAsync(true);

        // Act
        var result = await _sut.CreateAsync(new CreateTenantRequest("Outra Barbearia", "barbearia-do-ze"));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("barbearia-do-ze", result.Error);

        // Garante que o repositório NÃO foi chamado para criar
        _repoMock.Verify(r => r.CreateAsync(It.IsAny<Tenant>(), default), Times.Never);
    }

    [Theory]
    [InlineData("", "slug-valido")]
    [InlineData("   ", "slug-valido")]
    public async Task CreateAsync_WithEmptyName_ReturnsValidationError(string name, string slug)
    {
        // Act
        var result = await _sut.CreateAsync(new CreateTenantRequest(name, slug));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("nome", result.Error, StringComparison.OrdinalIgnoreCase);

        _repoMock.Verify(r => r.SlugExistsAsync(It.IsAny<string>(), default), Times.Never);
    }

    [Theory]
    [InlineData("Nome Válido", "")]
    [InlineData("Nome Válido", "   ")]
    public async Task CreateAsync_WithEmptySlug_ReturnsValidationError(string name, string slug)
    {
        // Act
        var result = await _sut.CreateAsync(new CreateTenantRequest(name, slug));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("slug", result.Error, StringComparison.OrdinalIgnoreCase);

        _repoMock.Verify(r => r.SlugExistsAsync(It.IsAny<string>(), default), Times.Never);
    }
}
