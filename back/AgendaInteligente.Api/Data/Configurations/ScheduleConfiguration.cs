using AgendaInteligente.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgendaInteligente.Api.Data.Configurations;

public sealed class ScheduleConfiguration : IEntityTypeConfiguration<Schedule>
{
    public void Configure(EntityTypeBuilder<Schedule> builder)
    {
        builder.ToTable("schedules");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.StartDateTime)
            .IsRequired();

        builder.Property(s => s.EndDateTime)
            .IsRequired();

        builder.Property(s => s.Status)
            .IsRequired();

        builder.Property(s => s.Notes)
            .HasMaxLength(1000);

        builder.Property(s => s.RecurrenceRule)
            .HasMaxLength(500);

        builder.Property(s => s.GoogleCalendarEventId)
            .HasMaxLength(255);

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        builder.Property(s => s.UpdatedAt)
            .IsRequired();

        // ── Índice para consultas de conflito de horário ───────────────────────
        // Consulta mais crítica do sistema: "existe agendamento para este profissional
        // neste intervalo de tempo?"
        builder.HasIndex(s => new { s.TenantId, s.ProfessionalId, s.StartDateTime, s.EndDateTime })
            .HasDatabaseName("ix_schedules_tenant_professional_datetime");

        // ── Relacionamento com Tenant ──────────────────────────────────────────
        builder.HasOne(s => s.Tenant)
            .WithMany(t => t.Schedules)
            .HasForeignKey(s => s.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── Relacionamento com Customer ────────────────────────────────────────
        builder.HasOne(s => s.Customer)
            .WithMany()
            .HasForeignKey(s => s.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Relacionamento com Professional ────────────────────────────────────
        builder.HasOne(s => s.Professional)
            .WithMany(p => p.Schedules)
            .HasForeignKey(s => s.ProfessionalId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Relacionamento com Service ─────────────────────────────────────────
        builder.HasOne(s => s.Service)
            .WithMany(sv => sv.Schedules)
            .HasForeignKey(s => s.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
