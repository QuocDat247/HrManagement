using HrManagement.Domain.Attendance.Records;
using HrManagement.Domain.Attendance.Timesheets;
using HrManagement.Domain.Employees;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

public sealed class MonthlyTimesheetDaySnapshotConfiguration
    : IEntityTypeConfiguration<MonthlyTimesheetDaySnapshot>
{
    public void Configure(
        EntityTypeBuilder<MonthlyTimesheetDaySnapshot> builder)
    {
        builder.ToTable(
            "MonthlyTimesheetDaySnapshots");

        builder.HasKey(
            snapshot =>
                snapshot.Id);

        builder.Property(
                snapshot =>
                    snapshot.Id)
            .ValueGeneratedNever();

        builder.Property(
                snapshot =>
                    snapshot.TimesheetPeriodId)
            .IsRequired();

        builder.Property(
                snapshot =>
                    snapshot.AttendanceRecordId)
            .IsRequired();

        builder.Property(
                snapshot =>
                    snapshot.EmployeeId)
            .IsRequired();

        builder.Property(
                snapshot =>
                    snapshot.WorkDate)
            .IsRequired();

        builder.Property(
                snapshot =>
                    snapshot.IsWorkingDay)
            .IsRequired();

        builder.Property(
                snapshot =>
                    snapshot.ExpectedPlannedMinutes)
            .IsRequired();

        builder.Property(
                snapshot =>
                    snapshot.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(
                snapshot =>
                    snapshot.WorkedMinutes)
            .IsRequired();

        builder.Property(
                snapshot =>
                    snapshot.LateMinutes)
            .IsRequired();

        builder.Property(
                snapshot =>
                    snapshot.EarlyLeaveMinutes)
            .IsRequired();

        builder.Property(
                snapshot =>
                    snapshot.CorrectionRevision)
            .HasDefaultValue(
                0)
            .IsRequired();

        builder.HasIndex(
                snapshot => new
                {
                    snapshot.TimesheetPeriodId,
                    snapshot.EmployeeId,
                    snapshot.WorkDate
                })
            .IsUnique()
            .HasDatabaseName(
                "UX_MonthlyTimesheetSnapshots_Period_Employee_Date");

        builder.HasIndex(
                snapshot => new
                {
                    snapshot.EmployeeId,
                    snapshot.WorkDate
                })
            .HasDatabaseName(
                "IX_MonthlyTimesheetSnapshots_Employee_Date");

        builder.HasOne<TimesheetPeriod>()
            .WithMany()
            .HasForeignKey(
                snapshot =>
                    snapshot.TimesheetPeriodId)
            .OnDelete(
                DeleteBehavior.Restrict);

        builder.HasOne<AttendanceRecord>()
            .WithMany()
            .HasForeignKey(
                snapshot =>
                    snapshot.AttendanceRecordId)
            .OnDelete(
                DeleteBehavior.Restrict);

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(
                snapshot =>
                    snapshot.EmployeeId)
            .OnDelete(
                DeleteBehavior.Restrict);
    }
}
