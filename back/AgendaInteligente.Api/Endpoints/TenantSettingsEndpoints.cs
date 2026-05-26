using AgendaInteligente.Api.Contracts.Requests;
using AgendaInteligente.Api.Contracts.Responses;
using AgendaInteligente.Api.Domain.Entities;
using AgendaInteligente.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AgendaInteligente.Api.Endpoints;

public static class TenantSettingsEndpoints
{
    public static void MapTenantSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/tenant-settings")
            .WithTags("Tenant Settings")
            .RequireAuthorization();

        group.MapGet("/", async (ITenantSettingsService service, CancellationToken ct) =>
        {
            var settings = await service.GetAsync(ct);
            if (settings is null)
                return Results.NotFound(new { message = "Configurações não encontradas para este Tenant." });

            var response = new TenantSettingsResponse(
                settings.Id, settings.WorkingHoursJson, settings.DaysOffJson,
                settings.ReminderLeadTimeHours, settings.ReengagementInactiveDays,
                settings.BotDisplayName, settings.WhatsAppPhoneNumber,
                settings.ConflictMessageTemplate,
                HasGeminiApiKey: !string.IsNullOrEmpty(settings.GeminiApiKey),
                GeminiModel: settings.GeminiModel,
                TimeZoneId: settings.TimeZoneId);

            return Results.Ok(response);
        });

        group.MapPut("/", async ([FromBody] SaveTenantSettingsRequest request, ITenantSettingsService service, CancellationToken ct) =>
        {
            var existingSettings = await service.GetAsync(ct);

            TenantSettings settings;

            if (existingSettings is null)
            {
                // Create
                var newSettings = new TenantSettings
                {
                    WorkingHoursJson         = request.WorkingHoursJson,
                    DaysOffJson              = request.DaysOffJson,
                    ReminderLeadTimeHours    = request.ReminderLeadTimeHours,
                    ReengagementInactiveDays = request.ReengagementInactiveDays,
                    BotDisplayName           = request.BotDisplayName,
                    WhatsAppPhoneNumber      = request.WhatsAppPhoneNumber,
                    ConflictMessageTemplate  = request.ConflictMessageTemplate
                };

                if (request.GeminiApiKey is not null)
                    newSettings.GeminiApiKey = string.IsNullOrEmpty(request.GeminiApiKey) ? null : request.GeminiApiKey;
                if (request.GeminiModel is not null)
                    newSettings.GeminiModel = request.GeminiModel;
                if (request.TimeZoneId is not null)
                    newSettings.TimeZoneId = request.TimeZoneId;

                settings = await service.CreateAsync(newSettings, ct);
            }
            else
            {
                // Update
                existingSettings.WorkingHoursJson         = request.WorkingHoursJson;
                existingSettings.DaysOffJson              = request.DaysOffJson;
                existingSettings.ReminderLeadTimeHours    = request.ReminderLeadTimeHours;
                existingSettings.ReengagementInactiveDays = request.ReengagementInactiveDays;
                existingSettings.BotDisplayName           = request.BotDisplayName;
                existingSettings.WhatsAppPhoneNumber      = request.WhatsAppPhoneNumber;
                existingSettings.ConflictMessageTemplate  = request.ConflictMessageTemplate;
                existingSettings.UpdatedAt                = DateTime.UtcNow;

                if (request.GeminiApiKey is not null)
                    existingSettings.GeminiApiKey = string.IsNullOrEmpty(request.GeminiApiKey)
                        ? null
                        : request.GeminiApiKey;
                if (request.GeminiModel is not null)
                    existingSettings.GeminiModel = request.GeminiModel;
                if (request.TimeZoneId is not null)
                    existingSettings.TimeZoneId = request.TimeZoneId;

                settings = await service.UpdateAsync(existingSettings, ct);
            }

            var response = new TenantSettingsResponse(
                settings.Id, settings.WorkingHoursJson, settings.DaysOffJson,
                settings.ReminderLeadTimeHours, settings.ReengagementInactiveDays,
                settings.BotDisplayName, settings.WhatsAppPhoneNumber,
                settings.ConflictMessageTemplate,
                HasGeminiApiKey: !string.IsNullOrEmpty(settings.GeminiApiKey),
                GeminiModel: settings.GeminiModel,
                TimeZoneId: settings.TimeZoneId);

            return Results.Ok(response);
        }).RequireAuthorization("RequireOwnerRole");
    }
}
