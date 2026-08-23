using HrManagement.Domain.Attendance.Expectations;
using HrManagement.Domain.Attendance.Records;
using HrManagement.Domain.Attendance.Schedules;
using HrManagement.Domain.Employees;
using HrManagement.Domain.Attendance.Calculations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

public sealed class AttendanceRecordConfiguration
    : IEntityTypeConfiguration<AttendanceRecord>
{
    public void Configure(
        EntityTypeBuilder<AttendanceRecord> builder)
    {
        builder.ToTable(
            "AttendanceRecords");

        builder.HasKey(
            record => record.Id);

        builder.Property(
                record => record.Id)
            .ValueGeneratedNever();

        builder.Property(
                record => record.EmployeeId)
            .IsRequired();

        builder.Property(
                record => record.EmploymentPeriodId)
            .IsRequired();

        builder.Property(
                record => record.WorkScheduleAssignmentId)
            .IsRequired();

        builder.Property(
                record => record.WorkScheduleId)
            .IsRequired();

        builder.Property(
                record => record.WorkDate)
            .IsRequired();

        builder.Property(
                record => record.TimeZoneId)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(
                record => record.IsWorkingDay)
            .IsRequired();

        builder.Property(
            record => record.ExpectedStartTime);

        builder.Property(
            record => record.ExpectedEndTime);

        builder.Property(
                record => record.ExpectedBreakMinutes)
            .IsRequired();

        builder.Property(
            record =>
                record.ExpectationSource)
            .HasConversion<int>()
            .HasDefaultValue(
                WorkExpectationSource.WeeklySchedule)
            .IsRequired();

        builder.Property(
            record =>
                record.ExpectationSourceId);

        builder.Property(
                record =>
                    record.ExpectationSourceName)
            .HasMaxLength(
                500);

        builder.Ignore(
            record => record.ExpectedPlannedMinutes);

        builder.Ignore(
            record => record.IsOvernight);

        builder.HasIndex(
                record => new
                {
                    record.EmployeeId,
                    record.WorkDate
                })
            .IsUnique()
            .HasDatabaseName(
                "UX_AttendanceRecords_Employee_WorkDate");

        builder.HasIndex(
                record => record.WorkDate)
            .HasDatabaseName(
                "IX_AttendanceRecords_WorkDate");

        builder.HasIndex(
                record => record.EmploymentPeriodId)
            .HasDatabaseName(
                "IX_AttendanceRecords_EmploymentPeriodId");

        builder.HasIndex(
                record => record.WorkScheduleAssignmentId)
            .HasDatabaseName(
                "IX_AttendanceRecords_WorkScheduleAssignmentId");

        builder.HasIndex(
                record => record.WorkScheduleId)
            .HasDatabaseName(
                "IX_AttendanceRecords_WorkScheduleId");

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(
                record => record.EmployeeId)
            .OnDelete(
                DeleteBehavior.Restrict);

        builder.HasOne<EmploymentPeriod>()
            .WithMany()
            .HasForeignKey(
                record => record.EmploymentPeriodId)
            .OnDelete(
                DeleteBehavior.Restrict);

        builder.HasOne<EmployeeWorkScheduleAssignment>()
            .WithMany()
            .HasForeignKey(
                record => record.WorkScheduleAssignmentId)
            .OnDelete(
                DeleteBehavior.Restrict);

        builder.HasOne<WorkSchedule>()
            .WithMany()
            .HasForeignKey(
                record => record.WorkScheduleId)
            .OnDelete(
                DeleteBehavior.Restrict);

        builder.Property(
        record =>
            record.Status)
            .HasConversion<int>()
            .HasDefaultValue(
                AttendanceCalculationStatus.NotCalculated)
            .IsRequired();

        builder.Property(
                record =>
                    record.WorkedMinutes)
            .HasDefaultValue(
                0)
            .IsRequired();

        builder.Property(
                record =>
                    record.LateMinutes)
            .HasDefaultValue(
                0)
            .IsRequired();

        builder.Property(
                record =>
                    record.EarlyLeaveMinutes)
            .HasDefaultValue(
                0)
            .IsRequired();
    }
}
