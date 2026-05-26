using AgendaInteligente.Api.Domain.Entities;
using AgendaInteligente.Api.Domain.Enums;

namespace AgendaInteligente.Api.Services.Interfaces;

public interface IProfessionalService
{
    Task<IReadOnlyList<Professional>> GetAllActiveAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Professional>> GetAllAsync(CancellationToken ct = default);
    Task<Professional?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<Professional> CreateAsync(
        string name, string email, string password,
        string? calendarColor = null,
        ProfessionalRole? role = null,
        bool canManageServices = false,
        CancellationToken ct = default);

    Task<Professional> UpdateAsync(
        Guid id, string name, string? calendarColor, bool isActive,
        ProfessionalRole? role = null,
        bool? canManageServices = null,
        CancellationToken ct = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Atualiza os horários individuais do profissional.
    /// Passar workingHoursJson = null remove o override e volta ao padrão do tenant.
    /// </summary>
    Task<Professional> UpdateWorkingHoursAsync(Guid id, string? workingHoursJson, CancellationToken ct = default);
}
