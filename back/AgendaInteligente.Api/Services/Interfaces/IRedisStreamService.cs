namespace AgendaInteligente.Api.Services.Interfaces;

public interface IRedisStreamService
{
    bool IsAvailable { get; }

    Task PublishInboundAsync(
        Guid   tenantId,
        string numeroRemetente,
        string texto,
        CancellationToken ct = default);
}
