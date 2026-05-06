using AgendaInteligente.Api.Contracts.Requests;
using AgendaInteligente.Api.Contracts.Responses;
using AgendaInteligente.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AgendaInteligente.Api.Endpoints;

public static class ProfessionalEndpoints
{
    public static void MapProfessionalEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/professionals")
            .WithTags("Professionals")
            .RequireAuthorization();

        group.MapGet("/", async (IProfessionalService service, CancellationToken ct) =>
        {
            var professionals = await service.GetAllActiveAsync(ct);
            var response = professionals.Select(p => new ProfessionalResponse(
                p.Id, p.Name, p.Email, p.Role, p.CalendarColor, p.IsActive, p.CreatedAt));
            return Results.Ok(response);
        });

        group.MapGet("/{id:guid}", async (Guid id, IProfessionalService service, CancellationToken ct) =>
        {
            var professional = await service.GetByIdAsync(id, ct);
            if (professional is null)
                return Results.NotFound();

            var response = new ProfessionalResponse(
                professional.Id, professional.Name, professional.Email,
                professional.Role, professional.CalendarColor, professional.IsActive, professional.CreatedAt);
            
            return Results.Ok(response);
        });

        group.MapPost("/", async ([FromBody] CreateProfessionalRequest request, IProfessionalService service, CancellationToken ct) =>
        {
            var professional = await service.CreateAsync(
                request.Name, request.Email, request.Password, request.CalendarColor, ct);

            var response = new ProfessionalResponse(
                professional.Id, professional.Name, professional.Email,
                professional.Role, professional.CalendarColor, professional.IsActive, professional.CreatedAt);

            return Results.Created($"/api/v1/professionals/{professional.Id}", response);
        })
        .RequireAuthorization("RequireOwnerRole");

        group.MapPut("/{id:guid}", async (Guid id, [FromBody] UpdateProfessionalRequest request, IProfessionalService service, CancellationToken ct) =>
        {
            try
            {
                var professional = await service.UpdateAsync(
                    id, request.Name, request.CalendarColor, request.IsActive, ct);

                var response = new ProfessionalResponse(
                    professional.Id, professional.Name, professional.Email,
                    professional.Role, professional.CalendarColor, professional.IsActive, professional.CreatedAt);

                return Results.Ok(response);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
        })
        .RequireAuthorization("RequireOwnerRole");

        group.MapDelete("/{id:guid}", async (Guid id, IProfessionalService service, CancellationToken ct) =>
        {
            var success = await service.DeleteAsync(id, ct);
            return success ? Results.NoContent() : Results.NotFound();
        })
        .RequireAuthorization("RequireOwnerRole");
    }
}
