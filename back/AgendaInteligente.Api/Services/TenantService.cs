using AgendaInteligente.Api.Common;
using AgendaInteligente.Api.Contracts.Requests;
using AgendaInteligente.Api.Contracts.Responses;
using AgendaInteligente.Api.Domain.Entities;
using AgendaInteligente.Api.Repositories.Interfaces;

namespace AgendaInteligente.Api.Services;

public sealed class TenantService
{
    private readonly ITenantRepository _repo;

    public TenantService(ITenantRepository repo) => _repo = repo;

    public async Task<ServiceResult<TenantResponse>> CreateAsync(
        CreateTenantRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return ServiceResult<TenantResponse>.Fail("O nome do estabelecimento é obrigatório.");

        if (string.IsNullOrWhiteSpace(request.Slug))
            return ServiceResult<TenantResponse>.Fail("O slug é obrigatório.");

        // Normaliza: lowercase + trim (ex: " Barbearia-XYZ " → "barbearia-xyz")
        var slug = request.Slug.Trim().ToLowerInvariant();

        if (await _repo.SlugExistsAsync(slug, ct))
            return ServiceResult<TenantResponse>.Fail(
                $"O slug '{slug}' já está em uso por outro estabelecimento.");

        var tenant = new Tenant
        {
            Name = request.Name.Trim(),
            Slug = slug
        };

        var created = await _repo.CreateAsync(tenant, ct);

        return ServiceResult<TenantResponse>.Success(
            new TenantResponse(created.Id, created.Name, created.Slug, created.IsActive, created.CreatedAt));
    }
}
