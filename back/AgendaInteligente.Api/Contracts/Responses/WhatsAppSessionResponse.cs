namespace AgendaInteligente.Api.Contracts.Responses;

public sealed record WhatsAppSessionResponse(string SessionId, string Status, string? QrCode);
