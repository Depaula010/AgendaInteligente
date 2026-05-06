using System.IdentityModel.Tokens.Jwt;
using AgendaInteligente.Api.Common;
using AgendaInteligente.Api.Contracts.Auth;
using AgendaInteligente.Api.Domain.Entities;
using AgendaInteligente.Api.Domain.Enums;
using AgendaInteligente.Api.Repositories.Interfaces;
using AgendaInteligente.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace AgendaInteligente.Api.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IProfessionalRepository> _repoMock;
    private readonly IOptions<JwtSettings> _jwtOptions;
    private readonly AuthService _sut; // System Under Test

    public AuthServiceTests()
    {
        _repoMock = new Mock<IProfessionalRepository>();

        var jwtSettings = new JwtSettings
        {
            Secret = "SuperSecretKeyForTestingTheAuthService123!@#",
            ExpiryMinutes = 60,
            Issuer = "TestIssuer",
            Audience = "TestAudience"
        };
        _jwtOptions = Options.Create(jwtSettings);

        _sut = new AuthService(_repoMock.Object, _jwtOptions);
    }

    [Fact]
    public async Task LoginAsync_DeveRetornarToken_QuandoCredenciaisForemValidas()
    {
        // Arrange
        var password = "mySecurePassword123";
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
        var tenantId = Guid.NewGuid();

        var professional = new Professional
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "John Doe",
            Email = "john@example.com",
            PasswordHash = passwordHash,
            Role = ProfessionalRole.Owner,
            IsActive = true
        };

        _repoMock.Setup(x => x.GetByEmailIgnoringQueryFilterAsync(professional.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(professional);

        var request = new LoginRequest(professional.Email, password);

        // Act
        var response = await _sut.LoginAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.Token.Should().NotBeNullOrWhiteSpace();
        response.Id.Should().Be(professional.Id);
        response.Email.Should().Be(professional.Email);
        response.Role.Should().Be(professional.Role);
        response.TenantId.Should().Be(tenantId);

        // Opcional: Validar se o token gerado contém as claims corretas
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(response.Token);
        
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == professional.Id.ToString());
        jwtToken.Claims.Should().Contain(c => c.Type == "tenant_id" && c.Value == tenantId.ToString());
        jwtToken.Claims.Should().Contain(c => c.Type == "role" && c.Value == ProfessionalRole.Owner.ToString());
    }

    [Fact]
    public async Task LoginAsync_DeveLancarException_QuandoEmailNaoExistir()
    {
        // Arrange
        _repoMock.Setup(x => x.GetByEmailIgnoringQueryFilterAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Professional?)null);

        var request = new LoginRequest("nonexistent@example.com", "anypassword");

        // Act
        var act = async () => await _sut.LoginAsync(request);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Credenciais inválidas.");
    }

    [Fact]
    public async Task LoginAsync_DeveLancarException_QuandoSenhaEstiverIncorreta()
    {
        // Arrange
        var professional = new Professional
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Name = "John Doe",
            Email = "john@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("correctPassword123"),
            Role = ProfessionalRole.Staff,
            IsActive = true
        };

        _repoMock.Setup(x => x.GetByEmailIgnoringQueryFilterAsync(professional.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(professional);

        var request = new LoginRequest(professional.Email, "wrongPassword123");

        // Act
        var act = async () => await _sut.LoginAsync(request);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Credenciais inválidas.");
    }
    
    [Fact]
    public async Task LoginAsync_DeveLancarException_QuandoProfissionalEstiverInativo()
    {
        // Arrange
        var professional = new Professional
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Name = "John Doe",
            Email = "john@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("correctPassword123"),
            Role = ProfessionalRole.Staff,
            IsActive = false // INATIVO
        };

        _repoMock.Setup(x => x.GetByEmailIgnoringQueryFilterAsync(professional.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(professional);

        var request = new LoginRequest(professional.Email, "correctPassword123");

        // Act
        var act = async () => await _sut.LoginAsync(request);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Credenciais inválidas.");
    }
}
