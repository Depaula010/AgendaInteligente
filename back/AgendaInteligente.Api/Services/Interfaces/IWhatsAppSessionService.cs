using AgendaInteligente.Api.Common;
using AgendaInteligente.Api.Contracts.Responses;

namespace AgendaInteligente.Api.Services.Interfaces;

public interface IWhatsAppSessionService
{
    Task<ServiceResult<WhatsAppSessionResponse>>      CreateAndConnectAsync(CancellationToken ct = default);
    Task<ServiceResult<WhatsAppSessionStatusResponse>> GetStatusAsync(CancellationToken ct = default);
    Task<ServiceResult<bool>>                          ReconnectAsync(CancellationToken ct = default);
    Task<ServiceResult<WhatsAppSessionStatsResponse>>  GetStatsAsync(CancellationToken ct = default);
}
