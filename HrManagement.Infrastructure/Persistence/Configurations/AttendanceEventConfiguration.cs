using HrManagement.Domain.Attendance.Records;
using HrManagement.Domain.Employees;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

public sealed class AttendanceEventConfiguration
    : IEntityTypeConfiguration<AttendanceEvent>
{
    public void Configure(
        EntityTypeBuilder<AttendanceEvent> builder)
    {
        builder.ToTable(
            "AttendanceEvents");

        builder.HasKey(
            attendanceEvent =>
                attendanceEvent.Id);

        builder.Property(
                attendanceEvent =>
                    attendanceEvent.Id)
            .ValueGeneratedNever();

        builder.Property(
                attendanceEvent =>
                    attendanceEvent.AttendanceRecordId)
            .IsRequired();

        builder.Property(
                attendanceEvent =>
                    attendanceEvent.EmployeeId)
            .IsRequired();

        builder.Property(
                attendanceEvent =>
                    attendanceEvent.EventType)
            .IsRequired();

        builder.Property(
                attendanceEvent =>
                    attendanceEvent.OccurredAtUtc)
            .HasConversion(
                value =>
                    value.Ticks,
                value =>
                    new DateTime(
                        value,
                        DateTimeKind.Utc))
            .IsRequired();

        builder.HasIndex(
                attendanceEvent =>
                    attendanceEvent.AttendanceRecordId)
            .HasDatabaseName(
                "IX_AttendanceEvents_AttendanceRecordId");

        builder.HasIndex(
                attendanceEvent => new
                {
                    attendanceEvent.AttendanceRecordId,
                    attendanceEvent.OccurredAtUtc
                })
            .HasDatabaseName(
                "IX_AttendanceEvents_Record_OccurredAtUtc");

        builder.HasIndex(
                attendanceEvent => new
                {
                    attendanceEvent.EmployeeId,
                    attendanceEvent.OccurredAtUtc
                })
            .HasDatabaseName(
                "IX_AttendanceEvents_Employee_OccurredAtUtc");

        builder.HasOne<AttendanceRecord>()
            .WithMany()
            .HasForeignKey(
                attendanceEvent =>
                    attendanceEvent.AttendanceRecordId)
            .OnDelete(
                DeleteBehavior.Restrict);

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(
                attendanceEvent =>
                    attendanceEvent.EmployeeId)
            .OnDelete(
                DeleteBehavior.Restrict);
    }
}
