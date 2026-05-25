namespace AgendaInteligente.Api.Services.Interfaces;

public interface IReengagementService
{
    Task ProcessAllTenantsAsync(CancellationToken ct = default);
}
