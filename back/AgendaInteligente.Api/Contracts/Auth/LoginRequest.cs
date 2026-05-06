namespace AgendaInteligente.Api.Contracts.Auth;

/// <summary>
/// Contrato de entrada para a rota de login do painel PWA (Barbeiro).
/// </summary>
public record LoginRequest(string Email, string Password);
