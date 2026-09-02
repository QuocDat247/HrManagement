using HrManagement.Domain.Attendance.Timesheets;
using HrManagement.Domain.Payroll.Periods;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HrManagement.Infrastructure.Persistence.Configurations;

public sealed class PayrollPeriodConfiguration
    : IEntityTypeConfiguration<PayrollPeriod>
{
    public void Configure(
        EntityTypeBuilder<PayrollPeriod> builder)
    {
        builder.ToTable(
            "PayrollPeriods");

        builder.HasKey(
            period =>
                period.Id);

        builder.Property(
                period =>
                    period.Id)
            .ValueGeneratedNever();

        builder.Property(
                period =>
                    period.TimesheetPeriodId)
            .IsRequired();

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
                "UX_PayrollPeriods_Year_Month");

        builder.HasIndex(
                period =>
                    period.TimesheetPeriodId)
            .IsUnique()
            .HasDatabaseName(
                "UX_PayrollPeriods_TimesheetPeriodId");

        builder.HasOne<TimesheetPeriod>()
            .WithMany()
            .HasForeignKey(
                period =>
                    period.TimesheetPeriodId)
            .OnDelete(
                DeleteBehavior.Restrict);
    }
}
