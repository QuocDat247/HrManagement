using HrManagement.Domain.Employees;
using HrManagement.Domain.Employees.Profiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

public sealed class EmployeeAddressConfiguration
    : IEntityTypeConfiguration<EmployeeAddress>
{
    public void Configure(
        EntityTypeBuilder<EmployeeAddress> builder)
    {
        builder.ToTable(
            "EmployeeAddresses");

        builder.HasKey(
            address => address.Id);

        builder.Property(
                address => address.Id)
            .ValueGeneratedNever();

        builder.Property(
                address => address.EmployeeId)
            .IsRequired();

        builder.Property(
                address => address.Type)
            .IsRequired();

        builder.Property(
                address => address.AddressLine)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(
                address => address.Ward)
            .HasMaxLength(150);

        builder.Property(
                address => address.District)
            .HasMaxLength(150);

        builder.Property(
                address => address.Province)
            .HasMaxLength(150);

        builder.Property(
                address => address.Country)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(
                address => address.PostalCode)
            .HasMaxLength(30);

        builder.HasIndex(
                address => new
                {
                    address.EmployeeId,
                    address.Type
                })
            .IsUnique();

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(
                address => address.EmployeeId)
            .OnDelete(
                DeleteBehavior.Cascade);
    }
}
