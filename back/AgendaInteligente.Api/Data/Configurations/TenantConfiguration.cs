using AgendaInteligente.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgendaInteligente.Api.Data.Configurations;

public sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(t => t.Slug)
            .IsRequired()
            .HasMaxLength(50);

        // Slug deve ser globalmente único (identifica o tenant na URL/subdomínio)
        builder.HasIndex(t => t.Slug)
            .IsUnique();

        builder.Property(t => t.CreatedAt)
            .IsRequired();
    }
}
