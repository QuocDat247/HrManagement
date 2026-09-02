using HrManagement.Domain.Employees;
using HrManagement.Domain.Payroll.Compensation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

public sealed class EmployeeCompensationConfiguration
    : IEntityTypeConfiguration<EmployeeCompensation>
{
    public void Configure(
        EntityTypeBuilder<EmployeeCompensation> builder)
    {
        builder.ToTable(
            "EmployeeCompensations");

        builder.HasKey(
            compensation =>
                compensation.Id);

        builder.Property(
                compensation =>
                    compensation.Id)
            .ValueGeneratedNever();

        builder.Property(
                compensation =>
                    compensation.EmployeeId)
            .IsRequired();

        builder.Property(
                compensation =>
                    compensation.EmploymentPeriodId)
            .IsRequired();

        builder.Property(
                compensation =>
                    compensation.EffectiveFrom)
            .IsRequired();

        builder.Property(
            compensation =>
                compensation.EffectiveTo);

        builder.Property(
                compensation =>
                    compensation.MonthlyBaseSalary)
            .HasPrecision(
                18,
                2)
            .IsRequired();

        builder.Property(
                compensation =>
                    compensation.CurrencyCode)
            .HasMaxLength(
                3)
            .IsFixedLength()
            .IsRequired();

        builder.Ignore(
            compensation =>
                compensation.IsOpen);

        builder.HasIndex(
                compensation => new
                {
                    compensation.EmployeeId,
                    compensation.EffectiveFrom
                })
            .HasDatabaseName(
                "IX_EmployeeCompensations_Employee_EffectiveFrom");

        builder.HasIndex(
                compensation => new
                {
                    compensation.EmploymentPeriodId,
                    compensation.EffectiveFrom
                })
            .IsUnique()
            .HasDatabaseName(
                "UX_EmployeeCompensations_EmploymentPeriod_EffectiveFrom");

        builder.HasIndex(
                compensation =>
                    compensation.EmploymentPeriodId)
            .IsUnique()
            .HasFilter(
                "\"EffectiveTo\" IS NULL")
            .HasDatabaseName(
                "UX_EmployeeCompensations_EmploymentPeriod_Open");

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(
                compensation =>
                    compensation.EmployeeId)
            .OnDelete(
                DeleteBehavior.Restrict);

        builder.HasOne<EmploymentPeriod>()
            .WithMany()
            .HasForeignKey(
                compensation =>
                    compensation.EmploymentPeriodId)
            .OnDelete(
                DeleteBehavior.Restrict);
    }
}
