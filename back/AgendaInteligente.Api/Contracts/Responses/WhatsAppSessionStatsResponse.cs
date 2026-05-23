namespace AgendaInteligente.Api.Contracts.Responses;

public sealed record WhatsAppSessionStatsResponse(
    string  SessionId,
    bool    IsActive,
    int     MessagesReceived,
    int     MessagesSent,
    int     WebhookErrors,
    int     CircuitBreakerTrips,
    int     ReconnectCount,
    string? ConnectedAt,
    int?    UptimeSeconds
);
