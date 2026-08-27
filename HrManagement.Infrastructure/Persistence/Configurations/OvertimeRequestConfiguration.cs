using HrManagement.Domain.Employees;
using HrManagement.Domain.Overtime.Requests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

public sealed class OvertimeRequestConfiguration
    : IEntityTypeConfiguration<OvertimeRequest>
{
    public void Configure(
        EntityTypeBuilder<OvertimeRequest> builder)
    {
        builder.ToTable(
            "OvertimeRequests");

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
                request => request.WorkDate)
            .IsRequired();

        builder.Property(
                request => request.RequestedMinutes)
            .IsRequired();

        builder.Property(
                request => request.Reason)
            .HasMaxLength(500);

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

        builder.Property(
                request => request.ApprovedMinutes);

        builder.HasIndex(
                request =>
                    request.EmploymentPeriodId)
            .HasDatabaseName(
                "IX_OvertimeRequests_EmploymentPeriodId");

        builder.HasIndex(
                request => new
                {
                    request.Status,
                    request.WorkDate
                })
            .HasDatabaseName(
                "IX_OvertimeRequests_Status_WorkDate");

        builder.HasIndex(
                request => new
                {
                    request.EmployeeId,
                    request.WorkDate
                })
            .IsUnique()
            .HasFilter(
                "\"Status\" IN (1, 2)")
            .HasDatabaseName(
                "UX_OvertimeRequests_Employee_WorkDate_Active");

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(
                request =>
                    request.EmployeeId)
            .OnDelete(
                DeleteBehavior.Restrict);

        builder.HasOne<EmploymentPeriod>()
            .WithMany()
            .HasForeignKey(
                request =>
                    request.EmploymentPeriodId)
            .OnDelete(
                DeleteBehavior.Restrict);
    }
}
