using AgendaInteligente.Api.Common;
using AgendaInteligente.Api.Domain.Exceptions;
using AgendaInteligente.Api.Models.AI;
using AgendaInteligente.Api.MultiTenancy;
using AgendaInteligente.Api.Services.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace AgendaInteligente.Api.Endpoints;

public static class AiEndpoints
{
    public static void MapAiEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ai")
            .RequireAuthorization()
            .WithTags("Artificial Intelligence");

        group.MapPost("/extract-intent", ExtractIntentAsync)
            .Produces<GeminiIntentResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);
    }

    private static async Task<IResult> ExtractIntentAsync(
        [FromBody] ExtractIntentRequest request,
        [FromServices] IAiOrchestratorService aiOrchestratorService,
        [FromServices] ITenantProvider tenantProvider)
    {
        var tenantId = tenantProvider.CurrentTenantId;
        if (!tenantId.HasValue)
            return Results.Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Message))
            return Results.BadRequest(new ErrorResponse("Mensagem inválida.", "Validation", "A mensagem não pode ser vazia."));

        try
        {
            var response = await aiOrchestratorService.ProcessUserMessageAsync(tenantId.Value, request.Message, request.History ?? new List<MessageHistory>());
            return Results.Ok(response);
        }
        catch (BusinessException ex)
        {
            return Results.BadRequest(new ErrorResponse(ex.Message, "BusinessRule"));
        }
        catch (GeminiIntegrationException ex)
        {
            return Results.Problem(
                title: "Falha na IA",
                detail: ex.Message,
                statusCode: StatusCodes.Status502BadGateway);
        }
    }
}

public class ExtractIntentRequest
{
    public string Message { get; set; } = string.Empty;
    public List<MessageHistory>? History { get; set; }
}
