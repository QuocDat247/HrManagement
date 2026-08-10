using HrManagement.Domain.Employees;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

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
    }
}
