namespace AgendaInteligente.Api.Domain.Interfaces;

/// <summary>
/// Marca uma entidade como pertencente a um Tenant.
/// O AppDbContext intercepta o SaveChangesAsync e preenche automaticamente
/// o TenantId nas entidades que implementam esta interface, garantindo
/// que nenhum insert ocorra sem isolamento multi-tenant.
/// </summary>
public interface IMustHaveTenant
{
    Guid TenantId { get; set; }
}
