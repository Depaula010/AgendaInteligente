using AgendaInteligente.Api.Contracts.Requests.Webhook;
using AgendaInteligente.Api.Filters;
using AgendaInteligente.Api.Services.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

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
            .WithDescription("Endpoint protegido por X-Api-Key que processa as mensagens recebidas no bot. Executa o loop completo: debounce → histórico Redis → Gemini → resposta ao cliente.")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> ProcessWhatsAppWebhookAsync(
        [FromBody] WebhookMessageRequest request,
        [FromServices] IWebhookService webhookService,
        CancellationToken ct)
    {
        try
        {
            await webhookService.ProcessWhatsAppMessageAsync(request, ct);
            return Results.Ok(new { message = "Webhook recebido e processado com sucesso." });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }
}
