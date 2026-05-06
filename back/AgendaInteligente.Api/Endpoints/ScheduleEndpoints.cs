using AgendaInteligente.Api.Contracts.Requests;
using AgendaInteligente.Api.Contracts.Responses;
using AgendaInteligente.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AgendaInteligente.Api.Endpoints;

public static class ScheduleEndpoints
{
    public static void MapScheduleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/schedules")
            .WithTags("Schedules")
            .RequireAuthorization();

        group.MapGet("/", async (
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] Guid? professionalId,
            IScheduleService service, CancellationToken ct) =>
        {
            var fromDate = from ?? DateTime.UtcNow.Date;
            var toDate = to ?? fromDate.AddDays(7);

            var schedules = professionalId.HasValue
                ? await service.GetByProfessionalAsync(professionalId.Value, fromDate, toDate, ct)
                : await service.GetByDateRangeAsync(fromDate, toDate, ct);

            var response = schedules.Select(s => new ScheduleResponse(
                s.Id, s.CustomerId, s.ProfessionalId, s.ServiceId,
                s.StartDateTime, s.EndDateTime, s.Status, s.Notes, s.CreatedAt));

            return Results.Ok(response);
        });

        group.MapGet("/{id:guid}", async (Guid id, IScheduleService service, CancellationToken ct) =>
        {
            var schedule = await service.GetByIdAsync(id, ct);
            if (schedule is null)
                return Results.NotFound();

            var response = new ScheduleResponse(
                schedule.Id, schedule.CustomerId, schedule.ProfessionalId, schedule.ServiceId,
                schedule.StartDateTime, schedule.EndDateTime, schedule.Status, schedule.Notes, schedule.CreatedAt);

            return Results.Ok(response);
        });

        group.MapPost("/", async ([FromBody] CreateScheduleRequest request, IScheduleService service, CancellationToken ct) =>
        {
            try
            {
                var schedule = await service.CreateAsync(
                    request.CustomerId, request.ProfessionalId, request.ServiceId,
                    request.StartDateTime, request.Notes, ct);

                var response = new ScheduleResponse(
                    schedule.Id, schedule.CustomerId, schedule.ProfessionalId, schedule.ServiceId,
                    schedule.StartDateTime, schedule.EndDateTime, schedule.Status, schedule.Notes, schedule.CreatedAt);

                return Results.Created($"/api/v1/schedules/{schedule.Id}", response);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { error = ex.Message });
            }
        });

        group.MapPut("/{id:guid}", async (Guid id, [FromBody] UpdateScheduleRequest request, IScheduleService service, CancellationToken ct) =>
        {
            try
            {
                var schedule = await service.UpdateAsync(
                    id, request.StartDateTime, request.Notes, ct);

                var response = new ScheduleResponse(
                    schedule.Id, schedule.CustomerId, schedule.ProfessionalId, schedule.ServiceId,
                    schedule.StartDateTime, schedule.EndDateTime, schedule.Status, schedule.Notes, schedule.CreatedAt);

                return Results.Ok(response);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { error = ex.Message });
            }
        });

        group.MapPatch("/{id:guid}/status", async (Guid id, [FromBody] UpdateScheduleStatusRequest request, IScheduleService service, CancellationToken ct) =>
        {
            var success = await service.UpdateStatusAsync(id, request.Status, ct);
            return success ? Results.NoContent() : Results.NotFound();
        });

        group.MapDelete("/{id:guid}", async (Guid id, IScheduleService service, CancellationToken ct) =>
        {
            var success = await service.DeleteAsync(id, ct);
            return success ? Results.NoContent() : Results.NotFound();
        });
    }
}
