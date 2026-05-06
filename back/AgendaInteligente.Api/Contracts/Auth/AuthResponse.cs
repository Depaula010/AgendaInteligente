using AgendaInteligente.Api.Domain.Enums;

namespace AgendaInteligente.Api.Contracts.Auth;

/// <summary>
/// Resposta de login bem-sucedido contendo o token JWT e dados essenciais do usuário.
/// </summary>
public record AuthResponse(
    string Token,
    Guid Id,
    string Name,
    string Email,
    ProfessionalRole Role,
    Guid TenantId
);
