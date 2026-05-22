using AgendaInteligente.Api.Domain.Entities;
using AgendaInteligente.Api.Domain.Enums;
using AgendaInteligente.Api.Repositories.Interfaces;
using AgendaInteligente.Api.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace AgendaInteligente.Api.Services;

public sealed class ProfessionalService : IProfessionalService
{
    private readonly IProfessionalRepository _repo;
    private readonly ILogger<ProfessionalService> _logger;

    public ProfessionalService(IProfessionalRepository repo, ILogger<ProfessionalService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public Task<IReadOnlyList<Professional>> GetAllActiveAsync(CancellationToken ct = default)
        => _repo.GetAllActiveAsync(ct);

    public Task<IReadOnlyList<Professional>> GetAllAsync(CancellationToken ct = default)
        => _repo.GetAllAsync(ct);

    public Task<Professional?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _repo.GetByIdAsync(id, ct);

    public async Task<Professional> CreateAsync(
        string name, string email, string passwordHash,
        string? calendarColor = null, CancellationToken ct = default)
    {
        // Verifica e-mail duplicado dentro do mesmo Tenant
        var existing = await _repo.GetByEmailAsync(email, ct);
        if (existing is not null)
            throw new InvalidOperationException($"Já existe um profissional com o e-mail '{email}' neste estabelecimento.");

        var professional = new Professional
        {
            Name          = name,
            Email         = email,
            PasswordHash  = passwordHash,
            CalendarColor = calendarColor,
            Role          = ProfessionalRole.Staff
        };

        _logger.LogInformation("Criando profissional '{Name}' ({Email})", name, email);
        return await _repo.CreateAsync(professional, ct);
    }

    public async Task<Professional> UpdateAsync(
        Guid id, string name, string? calendarColor, bool isActive,
        CancellationToken ct = default)
    {
        var professional = await _repo.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Profissional '{id}' não encontrado.");

        professional.Name          = name;
        professional.CalendarColor = calendarColor;
        professional.IsActive      = isActive;

        await _repo.UpdateAsync(professional, ct);
        _logger.LogInformation("Profissional '{Id}' atualizado.", id);
        return professional;
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("Desativando profissional '{Id}'.", id);
        return _repo.DeleteAsync(id, ct);
    }
}
