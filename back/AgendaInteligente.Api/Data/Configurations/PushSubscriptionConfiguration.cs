using AgendaInteligente.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgendaInteligente.Api.Data.Configurations;

public sealed class PushSubscriptionConfiguration : IEntityTypeConfiguration<PushSubscription>
{
    public void Configure(EntityTypeBuilder<PushSubscription> builder)
    {
        builder.ToTable("push_subscriptions");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Endpoint)
            .IsRequired()
            .HasMaxLength(2048);

        builder.Property(p => p.P256dh)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(p => p.Auth)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        // Endpoint é globalmente único — cada dispositivo tem um endpoint único emitido pelo push service
        builder.HasIndex(p => p.Endpoint).IsUnique();

        builder.HasOne(p => p.Tenant)
            .WithMany()
            .HasForeignKey(p => p.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Professional)
            .WithMany()
            .HasForeignKey(p => p.ProfessionalId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
