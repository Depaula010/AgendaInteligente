using AgendaInteligente.Api.Services.Interfaces;

namespace AgendaInteligente.Api.Services;

/// <summary>
/// Implementação passthrough usada quando a chave de criptografia não está configurada.
/// Dados são armazenados em plaintext — aceitável em dev/CI sem credenciais reais.
/// </summary>
public sealed class NullEncryptionService : IEncryptionService
{
    public string? Encrypt(string? plaintext) => plaintext;
    public string? Decrypt(string? ciphertext) => ciphertext;
}
