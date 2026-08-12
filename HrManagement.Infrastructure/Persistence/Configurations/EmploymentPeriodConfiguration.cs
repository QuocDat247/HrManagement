using HrManagement.Domain.Employees;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

public sealed class EmploymentPeriodConfiguration
    : IEntityTypeConfiguration<EmploymentPeriod>
{
    public void Configure(
        EntityTypeBuilder<EmploymentPeriod> builder)
    {
        builder.ToTable("EmploymentPeriods");

        builder.HasKey(period => period.Id);

        builder.Property(period => period.Id)
            .ValueGeneratedNever();

        builder.Property(period => period.EmployeeId)
            .IsRequired();

        builder.Property(period => period.StartDate)
            .IsRequired();

        builder.Property(period => period.EndDate);

        builder.Ignore(period => period.IsOpen);

        builder.HasIndex(period => new
        {
            period.EmployeeId,
            period.StartDate
        });

        builder.HasIndex(period => period.EmployeeId)
            .IsUnique()
            .HasDatabaseName(
                "UX_EmploymentPeriods_EmployeeId_Open")
            .HasFilter("\"EndDate\" IS NULL");

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(period => period.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
