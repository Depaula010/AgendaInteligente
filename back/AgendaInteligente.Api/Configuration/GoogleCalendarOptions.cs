namespace AgendaInteligente.Api.Configuration;

/// <summary>
/// Opções de configuração para a integração com o Google Calendar OAuth2.
/// Lidas da seção "GoogleCalendar" do appsettings.json.
/// </summary>
public sealed class GoogleCalendarOptions
{
    public const string SectionName = "GoogleCalendar";

    /// <summary>Client ID do projeto OAuth2 configurado no Google Cloud Console.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>Client Secret do projeto OAuth2 configurado no Google Cloud Console.</summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// ID do calendário que receberá os eventos. Normalmente "primary" para usar o
    /// calendário principal da conta autenticada.
    /// </summary>
    public string CalendarId { get; set; } = "primary";
}
