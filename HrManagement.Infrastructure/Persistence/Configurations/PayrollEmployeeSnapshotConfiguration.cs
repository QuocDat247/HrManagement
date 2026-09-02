using HrManagement.Domain.Employees;
using HrManagement.Domain.Payroll.Periods;
using HrManagement.Domain.Payroll.Snapshots;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

public sealed class PayrollEmployeeSnapshotConfiguration
    : IEntityTypeConfiguration<PayrollEmployeeSnapshot>
{
    public void Configure(
        EntityTypeBuilder<PayrollEmployeeSnapshot> builder)
    {
        builder.ToTable(
            "PayrollEmployeeSnapshots");

        builder.HasKey(
            snapshot =>
                snapshot.Id);

        builder.Property(
                snapshot =>
                    snapshot.Id)
            .ValueGeneratedNever();

        builder.Property(
                snapshot =>
                    snapshot.PayrollPeriodId)
            .IsRequired();

        builder.Property(
                snapshot =>
                    snapshot.EmployeeId)
            .IsRequired();

        builder.Property(
                snapshot =>
                    snapshot.EmployeeCode)
            .HasMaxLength(
                30)
            .IsRequired();

        builder.Property(
                snapshot =>
                    snapshot.EmployeeFullName)
            .HasMaxLength(
                200)
            .IsRequired();

        builder.Property(
                snapshot =>
                    snapshot.CurrencyCode)
            .HasMaxLength(
                3)
            .IsFixedLength()
            .IsRequired();

        builder.Property(
                snapshot =>
                    snapshot.BaseSalaryAmount)
            .HasPrecision(
                18,
                2)
            .IsRequired();

        builder.Property(
                snapshot =>
                    snapshot.ApprovedOvertimeMinutes)
            .IsRequired();

        builder.Property(
                snapshot =>
                    snapshot.PayableOvertimeMinutes)
            .IsRequired();

        builder.Property(
                snapshot =>
                    snapshot.OvertimeAmount)
            .HasPrecision(
                18,
                2)
            .IsRequired();

        builder.Property(
                snapshot =>
                    snapshot.GrossAmount)
            .HasPrecision(
                18,
                2)
            .IsRequired();

        builder.HasIndex(
                snapshot => new
                {
                    snapshot.PayrollPeriodId,
                    snapshot.EmployeeId
                })
            .IsUnique()
            .HasDatabaseName(
                "UX_PayrollEmployeeSnapshots_Period_Employee");

        builder.HasIndex(
                snapshot =>
                    snapshot.EmployeeId)
            .HasDatabaseName(
                "IX_PayrollEmployeeSnapshots_Employee");

        builder.HasOne<PayrollPeriod>()
            .WithMany()
            .HasForeignKey(
                snapshot =>
                    snapshot.PayrollPeriodId)
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
