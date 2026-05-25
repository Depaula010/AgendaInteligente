using AgendaInteligente.Api.Domain.Entities;
using AgendaInteligente.Api.Repositories.Interfaces;
using AgendaInteligente.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AgendaInteligente.Api.Tests.Services;

public class PasswordResetServiceTests
{
    private readonly Mock<IProfessionalRepository> _repoMock = new();
    private readonly Mock<IDistributedCache>       _cacheMock = new();
    private readonly IConfiguration               _config;
    private readonly PasswordResetService         _sut;

    public PasswordResetServiceTests()
    {
        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AppUrl"] = "http://localhost:5173",
                // SMTP deliberadamente vazio → usa stub de log
            })
            .Build();

        _sut = new PasswordResetService(
            _repoMock.Object,
            _cacheMock.Object,
            _config,
            NullLogger<PasswordResetService>.Instance);
    }

    // ── ForgotPasswordAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task ForgotPasswordAsync_EmailNaoCadastrado_NaoArmazenaToken()
    {
        _repoMock.Setup(r => r.GetByEmailIgnoringQueryFilterAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Professional?)null);

        await _sut.ForgotPasswordAsync("naoexiste@teste.com");

        _cacheMock.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ForgotPasswordAsync_EmailCadastrado_ArmazenaTokenNoCache()
    {
        var professional = BuildProfessional();
        _repoMock.Setup(r => r.GetByEmailIgnoringQueryFilterAsync(professional.Email, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(professional);

        await _sut.ForgotPasswordAsync(professional.Email);

        _cacheMock.Verify(c => c.SetAsync(
            It.Is<string>(k => k.StartsWith("pwd-reset:")),
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── ResetPasswordAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task ResetPasswordAsync_TokenVazio_LancaArgumentException()
    {
        var act = () => _sut.ResetPasswordAsync("", "NovaSenha123");
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("Token inválido.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345")] // menos de 6 caracteres
    public async Task ResetPasswordAsync_SenhaInvalida_LancaArgumentException(string senha)
    {
        var act = () => _sut.ResetPasswordAsync("token-qualquer", senha);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ResetPasswordAsync_TokenNaoEncontradoNoCache_LancaArgumentException()
    {
        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((byte[]?)null);

        var act = () => _sut.ResetPasswordAsync("token-invalido", "NovaSenha123");
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("Token inválido ou expirado.");
    }

    [Fact]
    public async Task ResetPasswordAsync_TokenValido_AtualizaHashEInvalidaToken()
    {
        var professional = BuildProfessional();
        var token    = Guid.NewGuid().ToString("N");
        var cacheKey = $"pwd-reset:{token}";
        var idBytes  = System.Text.Encoding.UTF8.GetBytes(professional.Id.ToString());

        _cacheMock.Setup(c => c.GetAsync(cacheKey, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(idBytes);

        _repoMock.Setup(r => r.GetByIdIgnoringQueryFilterAsync(professional.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(professional);

        Professional? captured = null;
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Professional>(), It.IsAny<CancellationToken>()))
                 .Callback<Professional, CancellationToken>((p, _) => captured = p)
                 .Returns(Task.CompletedTask);

        await _sut.ResetPasswordAsync(token, "NovaSenha123");

        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Professional>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.NotNull(captured);
        Assert.True(BCrypt.Net.BCrypt.Verify("NovaSenha123", captured!.PasswordHash));

        _cacheMock.Verify(c => c.RemoveAsync(cacheKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResetPasswordAsync_ProfissionalNaoEncontrado_LancaArgumentException()
    {
        var professional = BuildProfessional();
        var token    = Guid.NewGuid().ToString("N");
        var cacheKey = $"pwd-reset:{token}";
        var idBytes  = System.Text.Encoding.UTF8.GetBytes(professional.Id.ToString());

        _cacheMock.Setup(c => c.GetAsync(cacheKey, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(idBytes);

        _repoMock.Setup(r => r.GetByIdIgnoringQueryFilterAsync(professional.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Professional?)null);

        var act = () => _sut.ResetPasswordAsync(token, "NovaSenha123");
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("Profissional não encontrado.");
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static Professional BuildProfessional() => new()
    {
        Id           = Guid.NewGuid(),
        TenantId     = Guid.NewGuid(),
        Name         = "João",
        Email        = "joao@barbearia.com",
        PasswordHash = BCrypt.Net.BCrypt.HashPassword("senha-antiga"),
        IsActive     = true,
    };
}
