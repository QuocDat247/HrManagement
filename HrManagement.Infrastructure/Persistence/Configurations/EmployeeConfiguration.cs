using HrManagement.Domain.Employees;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HrManagement.Domain.Organization.Departments;
using HrManagement.Domain.Organization.Positions;

namespace HrManagement.Infrastructure.Persistence.Configurations;

public sealed class EmployeeConfiguration
    : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees");

        builder.HasKey(employee => employee.Id);

        builder.Property(employee => employee.Id)
            .ValueGeneratedNever();

        builder.Property(employee => employee.EmployeeCode)
            .IsRequired()
            .HasMaxLength(30);

        builder.HasIndex(employee => employee.EmployeeCode)
            .IsUnique();

        builder.Property(employee => employee.FullName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(employee => employee.Email)
            .HasMaxLength(256);

        builder.Property(employee => employee.PhoneNumber)
            .HasMaxLength(30);

        builder.Property(employee => employee.Department)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(employee => employee.Position)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(employee => employee.DateOfBirth);

        builder.Property(employee => employee.HireDate)
            .IsRequired();

        builder.Property(employee => employee.Status)
            .IsRequired();

        builder.Property(employee => employee.TerminationDate);

        builder.Property(employee => employee.DepartmentId)
        .IsRequired(false);

        builder.Property(employee => employee.PositionId)
            .IsRequired(false);

        builder.HasOne<Department>()
            .WithMany()
            .HasForeignKey(employee =>
        employee.DepartmentId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Position>()
            .WithMany()
            .HasForeignKey(employee =>
                employee.PositionId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
