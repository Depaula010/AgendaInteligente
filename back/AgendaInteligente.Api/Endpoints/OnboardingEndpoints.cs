using AgendaInteligente.Api.Contracts.Requests;
using AgendaInteligente.Api.Contracts.Responses;
using AgendaInteligente.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AgendaInteligente.Api.Endpoints;

public static class OnboardingEndpoints
{
    public static void MapOnboardingEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/onboarding", OnboardAsync)
            .WithName("Onboarding")
            .WithTags("Onboarding")
            .AllowAnonymous()
            .Produces<OnboardTenantResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    public static async Task<IResult> OnboardAsync(
        [FromBody] OnboardTenantRequest request,
        [FromServices] IOnboardingService service,
        CancellationToken ct)
    {
        var result = await service.OnboardAsync(request, ct);

        if (!result.IsSuccess)
        {
            var statusCode = result.Error.Contains("já está em uso")
                ? StatusCodes.Status409Conflict
                : StatusCodes.Status400BadRequest;

            return Results.Problem(detail: result.Error, statusCode: statusCode);
        }

        return Results.Created($"/api/v1/tenants/{result.Value.Slug}", result.Value);
    }
}
