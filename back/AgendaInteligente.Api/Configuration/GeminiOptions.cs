namespace AgendaInteligente.Api.Configuration;

/// <summary>
/// Configurações do Gemini provenientes do appsettings.json.
/// A API Key definida aqui serve como fallback caso o Tenant não tenha sua própria chave configurada.
/// </summary>
public class GeminiOptions
{
    public const string SectionName = "Gemini";

    public string? GlobalApiKey { get; set; }
}
