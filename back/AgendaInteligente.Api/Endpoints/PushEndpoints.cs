using System.Security.Claims;
using AgendaInteligente.Api.Configuration;
using AgendaInteligente.Api.Domain.Entities;
using AgendaInteligente.Api.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AgendaInteligente.Api.Endpoints;

public record SubscribePushRequest(string Endpoint, string P256dh, string Auth);
public record UnsubscribePushRequest(string Endpoint);

public static class PushEndpoints
{
    public static void MapPushEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/push")
            .WithTags("Push Notifications");

        // VAPID public key — sem autenticação (é uma chave pública por natureza)
        group.MapGet("/vapid-public-key", (IOptions<VapidOptions> vapid) =>
            Results.Ok(new { publicKey = vapid.Value.PublicKey }))
            .AllowAnonymous();

        group.MapPost("/subscribe", async (
            ClaimsPrincipal user,
            [FromBody] SubscribePushRequest request,
            IPushSubscriptionRepository repo,
            CancellationToken ct) =>
        {
            var professionalId = GetProfessionalId(user);
            if (professionalId is null)
                return Results.Unauthorized();

            var sub = new PushSubscription
            {
                ProfessionalId = professionalId.Value,
                Endpoint       = request.Endpoint,
                P256dh         = request.P256dh,
                Auth           = request.Auth,
            };

            await repo.UpsertAsync(sub, ct);
            return Results.NoContent();
        })
        .RequireAuthorization();

        group.MapDelete("/subscribe", async (
            ClaimsPrincipal user,
            [FromBody] UnsubscribePushRequest request,
            IPushSubscriptionRepository repo,
            CancellationToken ct) =>
        {
            var professionalId = GetProfessionalId(user);
            if (professionalId is null)
                return Results.Unauthorized();

            await repo.DeleteByEndpointAsync(request.Endpoint, ct);
            return Results.NoContent();
        })
        .RequireAuthorization();
    }

    private static Guid? GetProfessionalId(ClaimsPrincipal user)
    {
        // JWT usa "sub" — mapeado para NameIdentifier pelo middleware do ASP.NET Core
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier)
                 ?? user.FindFirstValue("sub");
        return Guid.TryParse(value, out var id) ? id : null;
    }
}
