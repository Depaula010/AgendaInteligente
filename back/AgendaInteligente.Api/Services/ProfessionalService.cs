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
        string name, string email, string password,
        string? calendarColor = null,
        ProfessionalRole? role = null,
        bool canManageServices = false,
        CancellationToken ct = default)
    {
        var existing = await _repo.GetByEmailAsync(email, ct);
        if (existing is not null)
            throw new InvalidOperationException($"Já existe um profissional com o e-mail '{email}' neste estabelecimento.");

        var professional = new Professional
        {
            Name              = name,
            Email             = email,
            PasswordHash      = BCrypt.Net.BCrypt.HashPassword(password),
            CalendarColor     = calendarColor,
            Role              = role ?? ProfessionalRole.Staff,
            CanManageServices = canManageServices
        };

        _logger.LogInformation("Criando profissional '{Name}' ({Email})", name, email);
        return await _repo.CreateAsync(professional, ct);
    }

    public async Task<Professional> UpdateAsync(
        Guid id, string name, string? calendarColor, bool isActive,
        ProfessionalRole? role = null,
        bool? canManageServices = null,
        CancellationToken ct = default)
    {
        var professional = await _repo.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Profissional '{id}' não encontrado.");

        professional.Name          = name;
        professional.CalendarColor = calendarColor;
        professional.IsActive      = isActive;

        if (role.HasValue)
            professional.Role = role.Value;
        if (canManageServices.HasValue)
            professional.CanManageServices = canManageServices.Value;

        await _repo.UpdateAsync(professional, ct);
        _logger.LogInformation("Profissional '{Id}' atualizado.", id);
        return professional;
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("Desativando profissional '{Id}'.", id);
        return _repo.DeleteAsync(id, ct);
    }

    public async Task<Professional> UpdateWorkingHoursAsync(
        Guid id, string? workingHoursJson, CancellationToken ct = default)
    {
        var professional = await _repo.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Profissional '{id}' não encontrado.");

        professional.WorkingHoursJson = workingHoursJson;

        await _repo.UpdateAsync(professional, ct);
        _logger.LogInformation("Horários individuais do profissional '{Id}' atualizados.", id);
        return professional;
    }
}
