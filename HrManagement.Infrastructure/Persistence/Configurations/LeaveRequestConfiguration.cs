using HrManagement.Domain.Employees;
using HrManagement.Domain.Leave.Requests;
using HrManagement.Domain.Leave.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

public sealed class LeaveRequestConfiguration
    : IEntityTypeConfiguration<LeaveRequest>
{
    public void Configure(
        EntityTypeBuilder<LeaveRequest> builder)
    {
        builder.ToTable(
            "LeaveRequests");

        builder.HasKey(
            request => request.Id);

        builder.Property(
                request => request.Id)
            .ValueGeneratedNever();

        builder.Property(
                request => request.EmployeeId)
            .IsRequired();

        builder.Property(
                request => request.EmploymentPeriodId)
            .IsRequired();

        builder.Property(
                request => request.LeaveTypeId)
            .IsRequired();

        builder.Property(
                request => request.StartDate)
            .IsRequired();

        builder.Property(
                request => request.EndDate)
            .IsRequired();

        builder.Property(
                request => request.Reason)
            .HasMaxLength(2000);

        builder.Property(
                request => request.SubmittedAtUtc)
            .HasConversion(
                value =>
                    value.Ticks,
                value =>
                    new DateTime(
                        value,
                        DateTimeKind.Utc))
            .IsRequired();

        builder.Property(
            request => request.Status)
                .HasConversion<int>()
                .IsRequired();

        builder.HasIndex(
                request => new
                {
                    request.EmployeeId,
                    request.StartDate,
                    request.EndDate
                })
            .HasDatabaseName(
                "IX_LeaveRequests_Employee_DateRange");

        builder.HasIndex(
                request => request.EmploymentPeriodId)
                    .HasDatabaseName(
                        "IX_LeaveRequests_EmploymentPeriodId");

        builder.HasIndex(
                request => request.LeaveTypeId)
            .HasDatabaseName(
                "IX_LeaveRequests_LeaveTypeId");

        builder.HasIndex(
                request => new
                {
                    request.Status,
                    request.SubmittedAtUtc
                })
            .HasDatabaseName(
                "IX_LeaveRequests_Status_SubmittedAtUtc");

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(
                request => request.EmployeeId)
            .OnDelete(
                DeleteBehavior.Restrict);

        builder.HasOne<EmploymentPeriod>()
            .WithMany()
            .HasForeignKey(
                request => request.EmploymentPeriodId)
            .OnDelete(
                DeleteBehavior.Restrict);

        builder.HasOne<LeaveType>()
            .WithMany()
            .HasForeignKey(
                request => request.LeaveTypeId)
            .OnDelete(
                DeleteBehavior.Restrict);
    }
}
