using AgendaInteligente.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgendaInteligente.Api.Data.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        // Formato E.164: +5511999998888 (max 15 dígitos + sinal)
        builder.Property(c => c.PhoneNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(c => c.Email)
            .HasMaxLength(254);

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        // ── Relacionamento ─────────────────────────────────────────────────────
        builder.HasOne(c => c.Tenant)
            .WithMany(t => t.Customers)
            .HasForeignKey(c => c.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── Índices ────────────────────────────────────────────────────────────
        // Optimiza queries filtradas por tenant (Global Query Filter vai usar isso)
        builder.HasIndex(c => c.TenantId);

        // Número de WhatsApp único por tenant (mesmo cliente não pode ter 2 registos)
        builder.HasIndex(c => new { c.TenantId, c.PhoneNumber })
            .IsUnique();
    }
}
