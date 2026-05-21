namespace AgendaInteligente.Api.Contracts.Responses;

public sealed record WhatsAppSessionStatusResponse(string Status, bool IsConnected, string? QrCode);
