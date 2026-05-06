using AgendaInteligente.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgendaInteligente.Api.Data.Configurations;

public sealed class WaitlistConfiguration : IEntityTypeConfiguration<Waitlist>
{
    public void Configure(EntityTypeBuilder<Waitlist> builder)
    {
        builder.ToTable("waitlist");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.DesiredDate)
            .IsRequired();

        builder.Property(w => w.Status)
            .IsRequired();

        builder.Property(w => w.CreatedAt)
            .IsRequired();

        // ── Índice para consultar a fila por data e profissional ───────────────
        builder.HasIndex(w => new { w.TenantId, w.ProfessionalId, w.DesiredDate, w.Status })
            .HasDatabaseName("ix_waitlist_tenant_professional_date_status");

        // ── Relacionamento com Tenant ──────────────────────────────────────────
        builder.HasOne(w => w.Tenant)
            .WithMany(t => t.Waitlists)
            .HasForeignKey(w => w.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── Relacionamento com Customer ────────────────────────────────────────
        builder.HasOne(w => w.Customer)
            .WithMany()
            .HasForeignKey(w => w.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Relacionamento com Professional (opcional) ─────────────────────────
        builder.HasOne(w => w.Professional)
            .WithMany(p => p.Waitlists)
            .HasForeignKey(w => w.ProfessionalId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        // ── Relacionamento com Service ─────────────────────────────────────────
        builder.HasOne(w => w.Service)
            .WithMany()
            .HasForeignKey(w => w.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
