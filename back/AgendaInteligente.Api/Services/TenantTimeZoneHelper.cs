using AgendaInteligente.Api.Domain.Entities;

namespace AgendaInteligente.Api.Services;

/// <summary>
/// Ponto único para resolver o fuso horário de um tenant.
/// Reutilize em qualquer serviço que precise converter datas UTC ↔ horário local.
/// </summary>
internal static class TenantTimeZoneHelper
{
    public static TimeZoneInfo GetTimeZone(TenantSettings? settings)
    {
        var id = settings?.TimeZoneId;
        if (string.IsNullOrWhiteSpace(id)) return TimeZoneInfo.Utc;
        try   { return TimeZoneInfo.FindSystemTimeZoneById(id); }
        catch { return TimeZoneInfo.Utc; }
    }
}
