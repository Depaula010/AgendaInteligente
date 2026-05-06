using AgendaInteligente.Api.Contracts.Auth;

namespace AgendaInteligente.Api.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
}
