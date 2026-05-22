namespace AgendaInteligente.Api.Contracts.Responses;

public record TenantSettingsResponse(
    Guid Id,
    string WorkingHoursJson,
    string DaysOffJson,
    int ReminderLeadTimeHours,
    int ReengagementInactiveDays,
    string? BotDisplayName,
    string? WhatsAppPhoneNumber,
    string? ConflictMessageTemplate,
    bool HasGeminiApiKey,
    string GeminiModel
);
