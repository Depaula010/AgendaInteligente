namespace AgendaInteligente.Api.Services.Interfaces;

public interface IPasswordResetService
{
    /// <summary>
    /// Gera um token de reset, armazena no Redis (TTL 1h) e envia o link por e-mail.
    /// Não lança exceção se o e-mail não existir (evita enumeração de usuários).
    /// </summary>
    Task ForgotPasswordAsync(string email, CancellationToken ct = default);

    /// <summary>
    /// Valida o token, redefine a senha e invalida o token.
    /// Lança <see cref="ArgumentException"/> se o token for inválido ou expirado.
    /// </summary>
    Task ResetPasswordAsync(string token, string newPassword, CancellationToken ct = default);
}
