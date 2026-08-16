using HrManagement.Domain.Employees;
using HrManagement.Domain.Employees.OrganizationAssignments;
using HrManagement.Domain.Organization.Departments;
using HrManagement.Domain.Organization.Positions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

public sealed class EmployeeOrganizationAssignmentConfiguration
    : IEntityTypeConfiguration<EmployeeOrganizationAssignment>
{
    public void Configure(
        EntityTypeBuilder<EmployeeOrganizationAssignment> builder)
    {
        builder.ToTable(
            "EmployeeOrganizationAssignments");

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
                assignment => assignment.DepartmentId)
            .IsRequired();

        builder.Property(
                assignment => assignment.DepartmentCode)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(
                assignment => assignment.DepartmentName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(
                assignment => assignment.PositionId)
            .IsRequired();

        builder.Property(
                assignment => assignment.PositionCode)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(
                assignment => assignment.PositionName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(
                assignment => assignment.StartDate)
            .IsRequired();

        builder.Property(
            assignment => assignment.EndDate);

        builder.Ignore(
            assignment => assignment.IsOpen);

        builder.HasIndex(
            assignment => new
            {
                assignment.EmployeeId,
                assignment.StartDate
            });

        builder.HasIndex(
                assignment => assignment.EmploymentPeriodId)
            .HasDatabaseName(
                "IX_EmployeeOrganizationAssignments_EmploymentPeriodId");

        builder.HasIndex(
                assignment => assignment.DepartmentId)
            .HasDatabaseName(
                "IX_EmployeeOrganizationAssignments_DepartmentId");

        builder.HasIndex(
                assignment => assignment.PositionId)
            .HasDatabaseName(
                "IX_EmployeeOrganizationAssignments_PositionId");

        builder.HasIndex(
                assignment => assignment.EmployeeId)
            .IsUnique()
            .HasDatabaseName(
                "UX_EmployeeOrganizationAssignments_EmployeeId_Open")
            .HasFilter(
                "\"EndDate\" IS NULL");

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(
                assignment => assignment.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<EmploymentPeriod>()
            .WithMany()
            .HasForeignKey(
                assignment => assignment.EmploymentPeriodId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Department>()
            .WithMany()
            .HasForeignKey(
                assignment => assignment.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Position>()
            .WithMany()
            .HasForeignKey(
                assignment => assignment.PositionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(
            assignment => assignment.IsBaseline)
    .           IsRequired();
    }
}
