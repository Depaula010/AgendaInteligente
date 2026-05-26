using AgendaInteligente.Api.Contracts.Requests;
using AgendaInteligente.Api.Contracts.Responses;
using AgendaInteligente.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AgendaInteligente.Api.Endpoints;

public static class ServiceCatalogEndpoints
{
    public static void MapServiceCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/services")
            .WithTags("Service Catalog")
            .RequireAuthorization();

        group.MapGet("/", async (IServiceCatalogService service, CancellationToken ct, [FromQuery] bool all = false) =>
        {
            var services = all
                ? await service.GetAllAsync(ct)
                : await service.GetAllActiveAsync(ct);
            var response = services.Select(s => new ServiceCatalogResponse(
                s.Id, s.Name, s.DurationMinutes, s.Price, s.Description, s.CalendarColor, s.IsActive, s.CreatedAt));
            return Results.Ok(response);
        });

        group.MapGet("/{id:guid}", async (Guid id, IServiceCatalogService service, CancellationToken ct) =>
        {
            var catalogService = await service.GetByIdAsync(id, ct);
            if (catalogService is null)
                return Results.NotFound();

            var response = new ServiceCatalogResponse(
                catalogService.Id, catalogService.Name, catalogService.DurationMinutes, catalogService.Price,
                catalogService.Description, catalogService.CalendarColor, catalogService.IsActive, catalogService.CreatedAt);

            return Results.Ok(response);
        });

        group.MapPost("/", async ([FromBody] CreateServiceCatalogRequest request, IServiceCatalogService service, CancellationToken ct) =>
        {
            try
            {
                var catalogService = await service.CreateAsync(
                    request.Name, request.DurationMinutes, request.Price, request.Description, request.CalendarColor, ct);

                var response = new ServiceCatalogResponse(
                    catalogService.Id, catalogService.Name, catalogService.DurationMinutes, catalogService.Price,
                    catalogService.Description, catalogService.CalendarColor, catalogService.IsActive, catalogService.CreatedAt);

                return Results.Created($"/api/v1/services/{catalogService.Id}", response);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .RequireAuthorization("RequireOwnerRole");

        group.MapPut("/{id:guid}", async (Guid id, [FromBody] UpdateServiceCatalogRequest request, IServiceCatalogService service, CancellationToken ct) =>
        {
            try
            {
                var catalogService = await service.UpdateAsync(
                    id, request.Name, request.DurationMinutes, request.Price,
                    request.Description, request.CalendarColor, request.IsActive, ct);

                var response = new ServiceCatalogResponse(
                    catalogService.Id, catalogService.Name, catalogService.DurationMinutes, catalogService.Price,
                    catalogService.Description, catalogService.CalendarColor, catalogService.IsActive, catalogService.CreatedAt);

                return Results.Ok(response);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .RequireAuthorization("RequireOwnerRole");

        group.MapDelete("/{id:guid}", async (Guid id, IServiceCatalogService service, CancellationToken ct) =>
        {
            var success = await service.DeleteAsync(id, ct);
            return success ? Results.NoContent() : Results.NotFound();
        })
        .RequireAuthorization("RequireOwnerRole");
    }
}
