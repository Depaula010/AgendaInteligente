using AgendaInteligente.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgendaInteligente.Api.Data.Configurations;

public sealed class ProfessionalConfiguration : IEntityTypeConfiguration<Professional>
{
    public void Configure(EntityTypeBuilder<Professional> builder)
    {
        builder.ToTable("professionals");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.Email)
            .IsRequired()
            .HasMaxLength(200);

        // Garante que não existam dois profissionais com o mesmo email no mesmo tenant.
        builder.HasIndex(p => new { p.TenantId, p.Email })
            .IsUnique();

        builder.Property(p => p.PasswordHash)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(p => p.Role)
            .IsRequired();

        builder.Property(p => p.CalendarColor)
            .HasMaxLength(7); // ex: "#4285F4"

        builder.Property(p => p.IsActive)
            .HasDefaultValue(true);

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        // ── Relacionamento com Tenant ──────────────────────────────────────────
        builder.HasOne(p => p.Tenant)
            .WithMany(t => t.Professionals)
            .HasForeignKey(p => p.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
