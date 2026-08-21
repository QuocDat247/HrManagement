using HrManagement.Domain.Leave.Requests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

public sealed class LeaveRequestStatusChangeConfiguration
    : IEntityTypeConfiguration<LeaveRequestStatusChange>
{
    public void Configure(
        EntityTypeBuilder<LeaveRequestStatusChange> builder)
    {
        builder.ToTable(
            "LeaveRequestStatusChanges");

        builder.HasKey(
            change => change.Id);

        builder.Property(
                change => change.Id)
            .ValueGeneratedNever();

        builder.Property(
                change => change.LeaveRequestId)
            .IsRequired();

        builder.Property(
                change => change.FromStatus)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(
                change => change.ToStatus)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(
                change => change.ChangedAtUtc)
            .HasConversion(
                value =>
                    value.Ticks,
                value =>
                    new DateTime(
                        value,
                        DateTimeKind.Utc))
            .IsRequired();

        builder.Property(
                change => change.ChangedByUserId)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(
                change => change.ChangedByUsername)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(
                change => change.Note)
            .HasMaxLength(1000);

        builder.HasIndex(
                change => new
                {
                    change.LeaveRequestId,
                    change.ChangedAtUtc
                })
            .HasDatabaseName(
                "IX_LeaveRequestStatusChanges_Request_ChangedAtUtc");

        builder.HasOne<LeaveRequest>()
            .WithMany()
            .HasForeignKey(
                change => change.LeaveRequestId)
            .OnDelete(
                DeleteBehavior.Restrict);
    }
}
