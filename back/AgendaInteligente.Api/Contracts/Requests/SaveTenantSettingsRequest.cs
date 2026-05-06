namespace AgendaInteligente.Api.Contracts.Requests;

public record SaveTenantSettingsRequest(
    string WorkingHoursJson,
    string DaysOffJson,
    int ReminderLeadTimeHours,
    int ReengagementInactiveDays,
    string? BotDisplayName,
    string? WhatsAppPhoneNumber
);
