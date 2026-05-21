using AgendaInteligente.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgendaInteligente.Api.Data.Configurations;

public sealed class TenantSettingsConfiguration : IEntityTypeConfiguration<TenantSettings>
{
    public void Configure(EntityTypeBuilder<TenantSettings> builder)
    {
        builder.ToTable("tenant_settings");

        builder.HasKey(ts => ts.Id);

        // Garante a unicidade da relação 1:1 com Tenant.
        builder.HasIndex(ts => ts.TenantId)
            .IsUnique();

        builder.Property(ts => ts.WorkingHoursJson)
            .IsRequired()
            .HasColumnType("text")
            .HasDefaultValue("[]");

        builder.Property(ts => ts.DaysOffJson)
            .IsRequired()
            .HasColumnType("text")
            .HasDefaultValue("[]");

        builder.Property(ts => ts.ReminderLeadTimeHours)
            .IsRequired()
            .HasDefaultValue(24);

        builder.Property(ts => ts.ReengagementInactiveDays)
            .IsRequired()
            .HasDefaultValue(30);

        builder.Property(ts => ts.BotDisplayName)
            .HasMaxLength(100);

        builder.Property(ts => ts.WhatsAppPhoneNumber)
            .HasMaxLength(20);

        builder.Property(ts => ts.BotSessionId);

        builder.Property(ts => ts.ConflictMessageTemplate)
            .HasColumnType("text");

        builder.Property(ts => ts.GeminiApiKey)
            .HasMaxLength(255);

        builder.Property(ts => ts.GeminiModel)
            .IsRequired()
            .HasMaxLength(100)
            .HasDefaultValue("gemini-2.5-flash-lite");

        builder.Property(ts => ts.CreatedAt)
            .IsRequired();

        builder.Property(ts => ts.UpdatedAt)
            .IsRequired();

        // ── Relacionamento 1:1 com Tenant ──────────────────────────────────────
        builder.HasOne(ts => ts.Tenant)
            .WithOne(t => t.Settings)
            .HasForeignKey<TenantSettings>(ts => ts.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
