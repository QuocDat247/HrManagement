using HrManagement.Domain.Attendance.Corrections;
using HrManagement.Domain.Attendance.Records;
using HrManagement.Domain.Employees;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HrManagement.Infrastructure.Persistence.Configurations;

public sealed class AttendanceCorrectionConfiguration
    : IEntityTypeConfiguration<AttendanceCorrection>
{
    public void Configure(
        EntityTypeBuilder<AttendanceCorrection> builder)
    {
        builder.ToTable(
            "AttendanceCorrections");

        builder.HasKey(
            correction =>
                correction.Id);

        builder.Property(
                correction =>
                    correction.Id)
            .ValueGeneratedNever();

        builder.Property(
                correction =>
                    correction.AttendanceRecordId)
            .IsRequired();

        builder.Property(
                correction =>
                    correction.EmployeeId)
            .IsRequired();

        builder.Property(
                correction =>
                    correction.AffectedEventId)
            .IsRequired();

        builder.Property(
                correction =>
                    correction.Revision)
            .IsRequired();

        builder.Property(
                correction =>
                    correction.Kind)
            .IsRequired();

        builder.Property(
            correction =>
                correction.BeforeEventType);

        builder.Property(
            correction =>
                correction.AfterEventType);

        var nullableUtcDateTimeConverter =
            new ValueConverter<DateTime?, long?>(
                value =>
                    value.HasValue
                        ? value.Value.Ticks
                        : (long?)null,
                value =>
                    value.HasValue
                        ? new DateTime(
                            value.Value,
                            DateTimeKind.Utc)
                        : (DateTime?)null);

        builder.Property(
                correction =>
                    correction.BeforeOccurredAtUtc)
            .HasConversion(
                nullableUtcDateTimeConverter);

        builder.Property(
                correction =>
                    correction.AfterOccurredAtUtc)
            .HasConversion(
                nullableUtcDateTimeConverter);

        builder.Property(
                correction =>
                    correction.Reason)
            .HasMaxLength(
                500)
            .IsRequired();

        builder.Property(
                correction =>
                    correction.CorrectedAtUtc)
            .HasConversion(
                value =>
                    value.Ticks,
                value =>
                    new DateTime(
                        value,
                        DateTimeKind.Utc))
            .IsRequired();

        builder.Property(
                correction =>
                    correction.ActorUserId)
            .HasMaxLength(
                100)
            .IsRequired();

        builder.Property(
                correction =>
                    correction.ActorUsername)
            .HasMaxLength(
                150)
            .IsRequired();

        builder.Ignore(
            correction =>
                correction.HasBeforeState);

        builder.Ignore(
            correction =>
                correction.HasAfterState);

        builder.HasIndex(
                correction => new
                {
                    correction.AttendanceRecordId,
                    correction.Revision
                })
            .IsUnique()
            .HasDatabaseName(
                "UX_AttendanceCorrections_Record_Revision");

        builder.HasIndex(
                correction => new
                {
                    correction.AttendanceRecordId,
                    correction.AffectedEventId,
                    correction.Revision
                })
            .HasDatabaseName(
                "IX_AttendanceCorrections_Record_Event_Revision");

        builder.HasIndex(
                correction => new
                {
                    correction.EmployeeId,
                    correction.CorrectedAtUtc
                })
            .HasDatabaseName(
                "IX_AttendanceCorrections_Employee_CorrectedAtUtc");

        builder.HasOne<AttendanceRecord>()
            .WithMany()
            .HasForeignKey(
                correction =>
                    correction.AttendanceRecordId)
            .OnDelete(
                DeleteBehavior.Restrict);

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(
                correction =>
                    correction.EmployeeId)
            .OnDelete(
                DeleteBehavior.Restrict);
    }
}
