using HrManagement.Domain.Overtime.Requests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

public sealed class OvertimeRequestStatusChangeConfiguration
    : IEntityTypeConfiguration<OvertimeRequestStatusChange>
{
    public void Configure(
        EntityTypeBuilder<OvertimeRequestStatusChange> builder)
    {
        builder.ToTable(
            "OvertimeRequestStatusChanges");

        builder.HasKey(
            change =>
                change.Id);

        builder.Property(
                change =>
                    change.Id)
            .ValueGeneratedNever();

        builder.Property(
                change =>
                    change.OvertimeRequestId)
            .IsRequired();

        builder.Property(
                change =>
                    change.PreviousStatus)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(
                change =>
                    change.NewStatus)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(
                change =>
                    change.ApprovedMinutes);

        builder.Property(
                change =>
                    change.ChangedAtUtc)
            .HasConversion(
                value =>
                    value.Ticks,
                value =>
                    new DateTime(
                        value,
                        DateTimeKind.Utc))
            .IsRequired();

        builder.Property(
                change =>
                    change.ChangedByUserId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(
                change =>
                    change.ChangedByUsername)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(
                change =>
                    change.Note)
            .HasMaxLength(500);

        builder.HasIndex(
                change => new
                {
                    change.OvertimeRequestId,
                    change.ChangedAtUtc
                })
            .HasDatabaseName(
                "IX_OvertimeRequestStatusChanges_Request_ChangedAtUtc");

        builder.HasOne<OvertimeRequest>()
            .WithMany()
            .HasForeignKey(
                change =>
                    change.OvertimeRequestId)
            .OnDelete(
                DeleteBehavior.Restrict);
    }
}
