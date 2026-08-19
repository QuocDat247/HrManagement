using HrManagement.Domain.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

public sealed class AuditEntryConfiguration
    : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(
        EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable(
            "AuditEntries");

        builder.HasKey(
            entry => entry.Id);

        builder.Property(
                entry => entry.Id)
            .ValueGeneratedNever();

        builder.Property(
                entry => entry.OccurredAtUtc)
            .HasConversion(
                value => value.Ticks,
                value =>
                    new DateTime(
                        value,
                        DateTimeKind.Utc))
            .IsRequired();

        builder.Property(
                entry => entry.ActorUserId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(
                entry => entry.ActorUsername)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(
                entry => entry.Action)
            .IsRequired();

        builder.Property(
                entry => entry.EntityType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(
                entry => entry.EntityId)
            .IsRequired();

        builder.Property(
            entry => entry.EmployeeId);

        builder.HasIndex(
            entry => entry.OccurredAtUtc);

        builder.HasIndex(
            entry => new
            {
                entry.EmployeeId,
                entry.OccurredAtUtc
            });

        builder.HasIndex(
            entry => new
            {
                entry.EntityType,
                entry.EntityId
            });
    }
}
