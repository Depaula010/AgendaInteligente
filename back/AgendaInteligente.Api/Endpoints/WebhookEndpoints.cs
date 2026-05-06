using AgendaInteligente.Api.Contracts.Requests.Webhook;
using AgendaInteligente.Api.Filters;
using AgendaInteligente.Api.Services.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System.Threading.Tasks;

namespace AgendaInteligente.Api.Endpoints;

public static class WebhookEndpoints
{
    public static void MapWebhookEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/webhooks")
            .WithTags("Webhooks")
            .AddEndpointFilter<ApiKeyAuthFilter>();

        group.MapPost("/whatsapp", ProcessWhatsAppWebhookAsync)
            .WithName("ProcessWhatsAppWebhook")
            .WithSummary("Recebe mensagens do WhatsApp via Baileys Node.js")
            .WithDescription("Endpoint protegido por X-Api-Key que processa as mensagens recebidas no bot.")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> ProcessWhatsAppWebhookAsync(
        [FromBody] WebhookMessageRequest request,
        [FromServices] IWebhookService webhookService)
    {
        try
        {
            await webhookService.ProcessWhatsAppMessageAsync(request);
            return Results.Ok(new { message = "Webhook recebido e processado com sucesso." });
        }
        catch (System.ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }
}
