using AgendaInteligente.Api.Domain.Entities;

namespace AgendaInteligente.Api.Services.Interfaces;

public interface IProfessionalService
{
    Task<IReadOnlyList<Professional>> GetAllActiveAsync(CancellationToken ct = default);
    Task<Professional?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Cria um novo profissional. O PasswordHash deve ser fornecido já hasheado (bcrypt).
    /// </summary>
    Task<Professional> CreateAsync(
        string name, string email, string passwordHash,
        string? calendarColor = null, CancellationToken ct = default);

    Task<Professional> UpdateAsync(
        Guid id, string name, string? calendarColor, bool isActive,
        CancellationToken ct = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
