using System.Security.Claims;

namespace AgendaInteligente.Api.MultiTenancy;

/// <summary>
/// Implementação de ITenantProvider baseada no HttpContext.
/// Extrai o TenantId do claim "tenant_id" do JWT (requisições PWA)
/// ou do header "x-tenant-id" (requisições internas do serviço Node.js/WhatsApp).
/// </summary>
public sealed class HttpContextTenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextTenantProvider(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    public Guid? CurrentTenantId
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            if (context is null) return null;

            // 1ª tentativa: claim do JWT (autenticação do PWA)
            var jwtClaim = context.User.FindFirstValue("tenant_id");
            if (Guid.TryParse(jwtClaim, out var tenantFromJwt))
                return tenantFromJwt;

            // 2ª tentativa: header customizado (webhooks do serviço Node.js)
            if (context.Request.Headers.TryGetValue("x-tenant-id", out var headerValue)
                && Guid.TryParse(headerValue, out var tenantFromHeader))
                return tenantFromHeader;

            return null;
        }
    }
}
