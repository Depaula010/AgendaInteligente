using AgendaInteligente.Api.Contracts.Requests;
using AgendaInteligente.Api.Contracts.Responses;
using AgendaInteligente.Api.Services;

namespace AgendaInteligente.Api.Endpoints;

public static class TenantEndpoints
{
    public static IEndpointRouteBuilder MapTenantEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/tenants")
                       .WithTags("Tenants");

        group.MapPost("/", CreateTenant)
             .WithName("CreateTenant")
             .WithSummary("Registar um novo estabelecimento no SaaS")
             .Produces<TenantResponse>(StatusCodes.Status201Created)
             .ProducesProblem(StatusCodes.Status409Conflict)
             .ProducesProblem(StatusCodes.Status400BadRequest);

        return app;
    }

    // ── Handlers ───────────────────────────────────────────────────────────────

    private static async Task<IResult> CreateTenant(
        CreateTenantRequest request,
        TenantService service,
        CancellationToken ct)
    {
        var result = await service.CreateAsync(request, ct);

        return result.IsSuccess
            ? Results.Created($"/api/v1/tenants/{result.Value.Id}", result.Value)
            : Results.Problem(
                detail: result.Error,
                statusCode: result.Error.Contains("obrigatório")
                    ? StatusCodes.Status400BadRequest
                    : StatusCodes.Status409Conflict);
    }
}
