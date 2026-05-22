namespace AgendaInteligente.Api.Configuration;

public sealed class VapidOptions
{
    public const string SectionName = "Vapid";

    /// <summary>mailto: do administrador — obrigatório pelo protocolo VAPID.</summary>
    public string Subject { get; set; } = "mailto:admin@agendainteligente.com.br";

    /// <summary>Chave pública VAPID em Base64Url. Gere com: npx web-push generate-vapid-keys</summary>
    public string PublicKey { get; set; } = "";

    /// <summary>Chave privada VAPID em Base64Url. Nunca exponha essa chave.</summary>
    public string PrivateKey { get; set; } = "";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(PublicKey) &&
        !string.IsNullOrWhiteSpace(PrivateKey) &&
        !PublicKey.StartsWith("REPLACE_ME");
}
