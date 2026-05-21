using AgendaInteligente.Api.Common;
using AgendaInteligente.Api.Contracts.Requests;
using AgendaInteligente.Api.Contracts.Responses;

namespace AgendaInteligente.Api.Services.Interfaces;

public interface IOnboardingService
{
    Task<ServiceResult<OnboardTenantResponse>> OnboardAsync(
        OnboardTenantRequest request,
        CancellationToken ct = default);
}
