using AgendaInteligente.Api.Common;
using AgendaInteligente.Api.Contracts.Requests;
using AgendaInteligente.Api.Contracts.Responses;
using AgendaInteligente.Api.Domain.Entities;
using AgendaInteligente.Api.Domain.Enums;
using AgendaInteligente.Api.Repositories.Interfaces;
using AgendaInteligente.Api.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace AgendaInteligente.Api.Services;

public sealed class OnboardingService : IOnboardingService
{
    private readonly IOnboardingRepository          _repo;
    private readonly ILogger<OnboardingService>     _logger;

    public OnboardingService(IOnboardingRepository repo, ILogger<OnboardingService> logger)
    {
        _repo   = repo;
        _logger = logger;
    }

    public async Task<ServiceResult<OnboardTenantResponse>> OnboardAsync(
        OnboardTenantRequest request,
        CancellationToken ct = default)
    {
        // ── Validação ──────────────────────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(request.TenantName))
            return ServiceResult<OnboardTenantResponse>.Fail("O nome do estabelecimento é obrigatório.");

        if (string.IsNullOrWhiteSpace(request.Slug))
            return ServiceResult<OnboardTenantResponse>.Fail("O slug é obrigatório.");

        if (string.IsNullOrWhiteSpace(request.OwnerName))
            return ServiceResult<OnboardTenantResponse>.Fail("O nome do proprietário é obrigatório.");

        if (string.IsNullOrWhiteSpace(request.OwnerEmail))
            return ServiceResult<OnboardTenantResponse>.Fail("O e-mail do proprietário é obrigatório.");

        if (string.IsNullOrWhiteSpace(request.OwnerPassword))
            return ServiceResult<OnboardTenantResponse>.Fail("A senha é obrigatória.");

        var slug = request.Slug.Trim().ToLowerInvariant();

        if (await _repo.SlugExistsAsync(slug, ct))
            return ServiceResult<OnboardTenantResponse>.Fail(
                $"O slug '{slug}' já está em uso por outro estabelecimento.");

        // ── Montagem das entidades ─────────────────────────────────────────────
        var tenant = new Tenant
        {
            Name = request.TenantName.Trim(),
            Slug = slug
        };

        // TenantId definido explicitamente: durante o onboarding não existe JWT,
        // portanto ITenantProvider.CurrentTenantId é null e o auto-fill não roda.
        var professional = new Professional
        {
            Name         = request.OwnerName.Trim(),
            Email        = request.OwnerEmail.Trim().ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.OwnerPassword),
            Role         = ProfessionalRole.Owner,
            TenantId     = tenant.Id
        };

        var settings = new TenantSettings
        {
            TenantId                 = tenant.Id,
            BotDisplayName           = request.TenantName.Trim(),
            WorkingHoursJson         = "[]",
            DaysOffJson              = "[]",
            ReminderLeadTimeHours    = 24,
            ReengagementInactiveDays = 30
        };

        // ── Persistência atômica ───────────────────────────────────────────────
        var (createdTenant, createdProfessional, _) =
            await _repo.CreateOnboardingAsync(tenant, professional, settings, ct);

        _logger.LogInformation(
            "Onboarding concluído. TenantId={TenantId}, Slug={Slug}, Owner={Email}",
            createdTenant.Id, createdTenant.Slug, createdProfessional.Email);

        return ServiceResult<OnboardTenantResponse>.Success(
            new OnboardTenantResponse(createdTenant.Id, createdProfessional.Id, createdTenant.Slug));
    }
}
