using AgendaInteligente.Api.Contracts.Requests;
using AgendaInteligente.Api.Domain.Entities;
using AgendaInteligente.Api.Domain.Enums;
using AgendaInteligente.Api.Repositories.Interfaces;
using AgendaInteligente.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AgendaInteligente.Api.Tests.Services;

public sealed class OnboardingServiceTests
{
    private readonly Mock<IOnboardingRepository> _repoMock;
    private readonly OnboardingService           _svc;

    public OnboardingServiceTests()
    {
        _repoMock = new Mock<IOnboardingRepository>();

        // Default: slug disponível
        _repoMock.Setup(r => r.SlugExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Default: devolve as próprias entidades (IDs já atribuídos no construtor)
        _repoMock.Setup(r => r.CreateOnboardingAsync(
                It.IsAny<Tenant>(), It.IsAny<Professional>(), It.IsAny<TenantSettings>(),
                It.IsAny<CancellationToken>()))
            .Returns((Tenant t, Professional p, TenantSettings s, CancellationToken _) =>
                Task.FromResult((t, p, s)));

        _svc = new OnboardingService(_repoMock.Object, new NullLogger<OnboardingService>());
    }

    // ── Happy path ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task OnboardAsync_WithValidData_ReturnsSuccessWithIds()
    {
        var result = await _svc.OnboardAsync(ValidRequest());

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value.TenantId);
        Assert.NotEqual(Guid.Empty, result.Value.ProfessionalId);
        Assert.Equal("barbearia-do-ze", result.Value.Slug);
    }

    [Fact]
    public async Task OnboardAsync_WithValidData_SetsOwnerRole()
    {
        Professional? createdProfessional = null;
        _repoMock.Setup(r => r.CreateOnboardingAsync(
                It.IsAny<Tenant>(), It.IsAny<Professional>(), It.IsAny<TenantSettings>(),
                It.IsAny<CancellationToken>()))
            .Callback<Tenant, Professional, TenantSettings, CancellationToken>((_, p, _, _) => createdProfessional = p)
            .Returns((Tenant t, Professional p, TenantSettings s, CancellationToken _) =>
                Task.FromResult((t, p, s)));

        await _svc.OnboardAsync(ValidRequest());

        Assert.NotNull(createdProfessional);
        Assert.Equal(ProfessionalRole.Owner, createdProfessional!.Role);
    }

    [Fact]
    public async Task OnboardAsync_WithValidData_HashesPassword()
    {
        Professional? createdProfessional = null;
        _repoMock.Setup(r => r.CreateOnboardingAsync(
                It.IsAny<Tenant>(), It.IsAny<Professional>(), It.IsAny<TenantSettings>(),
                It.IsAny<CancellationToken>()))
            .Callback<Tenant, Professional, TenantSettings, CancellationToken>((_, p, _, _) => createdProfessional = p)
            .Returns((Tenant t, Professional p, TenantSettings s, CancellationToken _) =>
                Task.FromResult((t, p, s)));

        await _svc.OnboardAsync(ValidRequest());

        Assert.NotNull(createdProfessional);
        Assert.NotEqual("senha123", createdProfessional!.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify("senha123", createdProfessional.PasswordHash));
    }

    [Fact]
    public async Task OnboardAsync_WithValidData_SetsBotDisplayNameFromTenantName()
    {
        TenantSettings? createdSettings = null;
        _repoMock.Setup(r => r.CreateOnboardingAsync(
                It.IsAny<Tenant>(), It.IsAny<Professional>(), It.IsAny<TenantSettings>(),
                It.IsAny<CancellationToken>()))
            .Callback<Tenant, Professional, TenantSettings, CancellationToken>((_, _, s, _) => createdSettings = s)
            .Returns((Tenant t, Professional p, TenantSettings s, CancellationToken _) =>
                Task.FromResult((t, p, s)));

        await _svc.OnboardAsync(ValidRequest());

        Assert.NotNull(createdSettings);
        Assert.Equal("Barbearia do Zé", createdSettings!.BotDisplayName);
    }

    [Fact]
    public async Task OnboardAsync_WithValidData_SetsDefaultTenantSettings()
    {
        TenantSettings? createdSettings = null;
        _repoMock.Setup(r => r.CreateOnboardingAsync(
                It.IsAny<Tenant>(), It.IsAny<Professional>(), It.IsAny<TenantSettings>(),
                It.IsAny<CancellationToken>()))
            .Callback<Tenant, Professional, TenantSettings, CancellationToken>((_, _, s, _) => createdSettings = s)
            .Returns((Tenant t, Professional p, TenantSettings s, CancellationToken _) =>
                Task.FromResult((t, p, s)));

        await _svc.OnboardAsync(ValidRequest());

        Assert.NotNull(createdSettings);
        Assert.Equal("[]", createdSettings!.WorkingHoursJson);
        Assert.Equal("[]", createdSettings.DaysOffJson);
        Assert.Equal(24, createdSettings.ReminderLeadTimeHours);
        Assert.Equal(30, createdSettings.ReengagementInactiveDays);
    }

    [Fact]
    public async Task OnboardAsync_WithValidData_SetsTenantIdOnProfessionalAndSettings()
    {
        Tenant?         createdTenant      = null;
        Professional?   createdProfessional = null;
        TenantSettings? createdSettings    = null;

        _repoMock.Setup(r => r.CreateOnboardingAsync(
                It.IsAny<Tenant>(), It.IsAny<Professional>(), It.IsAny<TenantSettings>(),
                It.IsAny<CancellationToken>()))
            .Callback<Tenant, Professional, TenantSettings, CancellationToken>((t, p, s, _) =>
            {
                createdTenant      = t;
                createdProfessional = p;
                createdSettings    = s;
            })
            .Returns((Tenant t, Professional p, TenantSettings s, CancellationToken _) =>
                Task.FromResult((t, p, s)));

        await _svc.OnboardAsync(ValidRequest());

        Assert.NotNull(createdTenant);
        Assert.Equal(createdTenant!.Id, createdProfessional!.TenantId);
        Assert.Equal(createdTenant.Id,  createdSettings!.TenantId);
    }

    [Fact]
    public async Task OnboardAsync_NormalizesSlugToLowercase()
    {
        var request = ValidRequest() with { Slug = "BARBEARIA-DO-ZE" };

        var result = await _svc.OnboardAsync(request);

        Assert.True(result.IsSuccess);
        Assert.Equal("barbearia-do-ze", result.Value.Slug);
    }

    // ── Slug duplicado ───────────────────────────────────────────────────────────

    [Fact]
    public async Task OnboardAsync_WithDuplicateSlug_ReturnsFailResult()
    {
        _repoMock.Setup(r => r.SlugExistsAsync("barbearia-do-ze", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _svc.OnboardAsync(ValidRequest());

        Assert.False(result.IsSuccess);
        Assert.Contains("já está em uso", result.Error);
        _repoMock.Verify(r => r.CreateOnboardingAsync(
            It.IsAny<Tenant>(), It.IsAny<Professional>(), It.IsAny<TenantSettings>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Campos obrigatórios ──────────────────────────────────────────────────────

    [Theory]
    [InlineData("", "slug", "Owner", "owner@test.com", "senha123", "nome do estabelecimento")]
    [InlineData("Tenant", "", "Owner", "owner@test.com", "senha123", "slug")]
    [InlineData("Tenant", "slug", "", "owner@test.com", "senha123", "nome do proprietário")]
    [InlineData("Tenant", "slug", "Owner", "", "senha123", "e-mail")]
    [InlineData("Tenant", "slug", "Owner", "owner@test.com", "", "senha")]
    public async Task OnboardAsync_WithMissingRequiredField_ReturnsFailResult(
        string tenantName, string slug, string ownerName, string ownerEmail, string ownerPassword,
        string expectedErrorFragment)
    {
        var request = new OnboardTenantRequest(tenantName, slug, ownerName, ownerEmail, ownerPassword);

        var result = await _svc.OnboardAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Contains(expectedErrorFragment, result.Error, StringComparison.OrdinalIgnoreCase);
        _repoMock.Verify(r => r.CreateOnboardingAsync(
            It.IsAny<Tenant>(), It.IsAny<Professional>(), It.IsAny<TenantSettings>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Helper ───────────────────────────────────────────────────────────────────

    private static OnboardTenantRequest ValidRequest() => new(
        TenantName    : "Barbearia do Zé",
        Slug          : "barbearia-do-ze",
        OwnerName     : "Zé da Silva",
        OwnerEmail    : "ze@barbearia.com",
        OwnerPassword : "senha123"
    );
}
