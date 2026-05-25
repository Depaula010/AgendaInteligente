using System.Security.Cryptography;
using System.Text;
using AgendaInteligente.Api.Services.Interfaces;

namespace AgendaInteligente.Api.Services;

/// <summary>
/// AES-256-GCM com nonce aleatório de 12 bytes e tag de 16 bytes.
/// Formato do ciphertext (Base64): [12 bytes nonce][16 bytes tag][N bytes ciphertext].
/// </summary>
public sealed class AesEncryptionService : IEncryptionService
{
    private readonly byte[] _key;

    public AesEncryptionService(string base64Key)
    {
        _key = Convert.FromBase64String(base64Key);
        if (_key.Length != 32)
            throw new ArgumentException("A chave AES deve ter 256 bits (32 bytes em Base64).", nameof(base64Key));
    }

    public string? Encrypt(string? plaintext)
    {
        if (plaintext is null) return null;

        var nonce      = new byte[AesGcm.NonceByteSizes.MaxSize]; // 12 bytes
        RandomNumberGenerator.Fill(nonce);

        var plaintextBytes  = Encoding.UTF8.GetBytes(plaintext);
        var ciphertextBytes = new byte[plaintextBytes.Length];
        var tag             = new byte[AesGcm.TagByteSizes.MaxSize]; // 16 bytes

        using var aes = new AesGcm(_key, AesGcm.TagByteSizes.MaxSize);
        aes.Encrypt(nonce, plaintextBytes, ciphertextBytes, tag);

        // nonce (12) + tag (16) + ciphertext
        var combined = new byte[nonce.Length + tag.Length + ciphertextBytes.Length];
        nonce.CopyTo(combined, 0);
        tag.CopyTo(combined, nonce.Length);
        ciphertextBytes.CopyTo(combined, nonce.Length + tag.Length);

        return Convert.ToBase64String(combined);
    }

    public string? Decrypt(string? ciphertext)
    {
        if (ciphertext is null) return null;

        byte[] combined;
        try { combined = Convert.FromBase64String(ciphertext); }
        catch
        {
            // Valor ainda não criptografado (plaintext legado) — retorna como está
            return ciphertext;
        }

        const int nonceSize = 12;
        const int tagSize   = 16;

        if (combined.Length < nonceSize + tagSize)
            return ciphertext; // Muito curto para ser criptografado — retorna como está

        var nonce           = combined[..nonceSize];
        var tag             = combined[nonceSize..(nonceSize + tagSize)];
        var ciphertextBytes = combined[(nonceSize + tagSize)..];
        var plaintextBytes  = new byte[ciphertextBytes.Length];

        try
        {
            using var aes = new AesGcm(_key, AesGcm.TagByteSizes.MaxSize);
            aes.Decrypt(nonce, ciphertextBytes, tag, plaintextBytes);
            return Encoding.UTF8.GetString(plaintextBytes);
        }
        catch (CryptographicException)
        {
            // Tag inválida — valor armazenado em plaintext (legado), retorna como está
            return ciphertext;
        }
    }
}
