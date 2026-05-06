using AgendaInteligente.Api.Domain.Entities;
using AgendaInteligente.Api.Domain.Interfaces;
using AgendaInteligente.Api.MultiTenancy;
using Microsoft.EntityFrameworkCore;


namespace AgendaInteligente.Api.Data;

public sealed class AppDbContext : DbContext
{
    private readonly ITenantProvider _tenantProvider;

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantProvider tenantProvider)
        : base(options)
    {
        _tenantProvider = tenantProvider;
    }

    // ── DbSets ─────────────────────────────────────────────────────────────────
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Professional> Professionals => Set<Professional>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<Schedule> Schedules => Set<Schedule>();
    public DbSet<TenantSettings> TenantSettings => Set<TenantSettings>();
    public DbSet<Waitlist> Waitlists => Set<Waitlist>();

    // ── Model Configuration ────────────────────────────────────────────────────
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Aplica automaticamente todos os IEntityTypeConfiguration<T> da assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // ── Global Query Filters: Isolamento Multi-Tenant ─────────────────────
        // Todas as queries nas entidades abaixo são automaticamente filtradas pelo
        // TenantId do contexto atual, impedindo que um tenant veja dados de outro.
        //
        // Quando CurrentTenantId for null (migrations, background jobs sem contexto
        // HTTP), o filtro é desativado — seguro pois esses contextos não são
        // acessíveis pelos clientes finais.

        modelBuilder.Entity<Customer>()
            .HasQueryFilter(e =>
                !_tenantProvider.CurrentTenantId.HasValue ||
                e.TenantId == _tenantProvider.CurrentTenantId.Value);

        modelBuilder.Entity<Professional>()
            .HasQueryFilter(e =>
                !_tenantProvider.CurrentTenantId.HasValue ||
                e.TenantId == _tenantProvider.CurrentTenantId.Value);

        modelBuilder.Entity<Service>()
            .HasQueryFilter(e =>
                !_tenantProvider.CurrentTenantId.HasValue ||
                e.TenantId == _tenantProvider.CurrentTenantId.Value);

        modelBuilder.Entity<Schedule>()
            .HasQueryFilter(e =>
                !_tenantProvider.CurrentTenantId.HasValue ||
                e.TenantId == _tenantProvider.CurrentTenantId.Value);

        modelBuilder.Entity<TenantSettings>()
            .HasQueryFilter(e =>
                !_tenantProvider.CurrentTenantId.HasValue ||
                e.TenantId == _tenantProvider.CurrentTenantId.Value);

        modelBuilder.Entity<Waitlist>()
            .HasQueryFilter(e =>
                !_tenantProvider.CurrentTenantId.HasValue ||
                e.TenantId == _tenantProvider.CurrentTenantId.Value);
    }

    // ── Auto-preenchimento de TenantId ────────────────────────────────────────
    // Antes de qualquer SaveChanges, garante que toda entidade marcada com
    // IMustHaveTenant receba o TenantId do contexto atual. Se o TenantId
    // já estiver preenchido (Guid != Empty), o valor existente é preservado.
    // Isso torna os Services e Repositories completamente agnósticos ao TenantId.
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.CurrentTenantId;

        if (tenantId.HasValue)
        {
            foreach (var entry in ChangeTracker.Entries<IMustHaveTenant>()
                         .Where(e => e.State == EntityState.Added
                                  && e.Entity.TenantId == Guid.Empty))
            {
                entry.Entity.TenantId = tenantId.Value;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
