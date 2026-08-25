using HrManagement.Domain.Attendance.Timesheets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HrManagement.Infrastructure.Persistence.Configurations;

public sealed class TimesheetPeriodConfiguration
    : IEntityTypeConfiguration<TimesheetPeriod>
{
    public void Configure(
        EntityTypeBuilder<TimesheetPeriod> builder)
    {
        builder.ToTable(
            "TimesheetPeriods");

        builder.HasKey(
            period =>
                period.Id);

        builder.Property(
                period =>
                    period.Id)
            .ValueGeneratedNever();

        builder.Property(
                period =>
                    period.Year)
            .IsRequired();

        builder.Property(
                period =>
                    period.Month)
            .IsRequired();

        builder.Property(
            period =>
                period.Status)
        .HasConversion<int>()
        .IsRequired();

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
                period =>
                    period.ClosedAtUtc)
            .HasConversion(
                nullableUtcDateTimeConverter);

        builder.Property(
                period =>
                    period.ClosedByUserId)
            .HasMaxLength(
                100);

        builder.Property(
                period =>
                    period.ClosedByUsername)
            .HasMaxLength(
                150);

        builder.Ignore(
            period =>
                period.StartDate);

        builder.Ignore(
            period =>
                period.EndDate);

        builder.Ignore(
            period =>
                period.IsClosed);

        builder.HasIndex(
                period => new
                {
                    period.Year,
                    period.Month
                })
            .IsUnique()
            .HasDatabaseName(
                "UX_TimesheetPeriods_Year_Month");
    }
}
