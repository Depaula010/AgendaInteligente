namespace AgendaInteligente.Api.Contracts.Requests;

public record SaveTenantSettingsRequest(
    string WorkingHoursJson,
    string DaysOffJson,
    int ReminderLeadTimeHours,
    int ReengagementInactiveDays,
    string? BotDisplayName,
    string? WhatsAppPhoneNumber,
    /// <summary>
    /// Template da mensagem de conflito. Use {alternatives} para injetar os horários disponíveis.
    /// Deixe nulo para usar o template padrão do sistema.
    /// </summary>
    string? ConflictMessageTemplate = null,
    /// <summary>null = não alterar; "" = remover; valor = definir nova chave</summary>
    string? GeminiApiKey = null,
    /// <summary>null = não alterar</summary>
    string? GeminiModel = null
);
