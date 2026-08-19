using HrManagement.Domain.Attendance.Schedules;
using HrManagement.Domain.Employees;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

public sealed class EmployeeWorkScheduleAssignmentConfiguration
    : IEntityTypeConfiguration<EmployeeWorkScheduleAssignment>
{
    public void Configure(
        EntityTypeBuilder<EmployeeWorkScheduleAssignment> builder)
    {
        builder.ToTable(
            "EmployeeWorkScheduleAssignments");

        builder.HasKey(
            assignment => assignment.Id);

        builder.Property(
                assignment => assignment.Id)
            .ValueGeneratedNever();

        builder.Property(
                assignment => assignment.EmployeeId)
            .IsRequired();

        builder.Property(
                assignment => assignment.EmploymentPeriodId)
            .IsRequired();

        builder.Property(
                assignment => assignment.WorkScheduleId)
            .IsRequired();

        builder.Property(
                assignment => assignment.EffectiveFrom)
            .IsRequired();

        builder.Property(
            assignment => assignment.EffectiveTo);

        builder.Ignore(
            assignment => assignment.IsOpen);

        builder.HasIndex(
                assignment => new
                {
                    assignment.EmployeeId,
                    assignment.EffectiveFrom
                })
            .HasDatabaseName(
                "IX_EmployeeWorkScheduleAssignments_Employee_EffectiveFrom");

        builder.HasIndex(
                assignment => assignment.EmploymentPeriodId)
            .HasDatabaseName(
                "IX_EmployeeWorkScheduleAssignments_EmploymentPeriodId");

        builder.HasIndex(
                assignment => assignment.WorkScheduleId)
            .HasDatabaseName(
                "IX_EmployeeWorkScheduleAssignments_WorkScheduleId");

        builder.HasIndex(
                assignment => assignment.EmployeeId)
            .IsUnique()
            .HasDatabaseName(
                "UX_EmployeeWorkScheduleAssignments_EmployeeId_Open")
            .HasFilter(
                "\"EffectiveTo\" IS NULL");

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(
                assignment => assignment.EmployeeId)
            .OnDelete(
                DeleteBehavior.Restrict);

        builder.HasOne<EmploymentPeriod>()
            .WithMany()
            .HasForeignKey(
                assignment => assignment.EmploymentPeriodId)
            .OnDelete(
                DeleteBehavior.Restrict);

        builder.HasOne<WorkSchedule>()
            .WithMany()
            .HasForeignKey(
                assignment => assignment.WorkScheduleId)
            .OnDelete(
                DeleteBehavior.Restrict);
    }
}
