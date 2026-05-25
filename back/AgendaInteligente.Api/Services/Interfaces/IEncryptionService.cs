namespace AgendaInteligente.Api.Services.Interfaces;

/// <summary>
/// Criptografia simétrica para dados sensíveis em repouso (ex: refresh tokens OAuth2).
/// </summary>
public interface IEncryptionService
{
    /// <summary>Criptografa o valor. Retorna null se o input for null.</summary>
    string? Encrypt(string? plaintext);

    /// <summary>Decriptografa o valor. Retorna null se o input for null.</summary>
    string? Decrypt(string? ciphertext);
}
