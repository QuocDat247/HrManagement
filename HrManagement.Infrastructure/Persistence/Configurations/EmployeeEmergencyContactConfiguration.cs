using HrManagement.Domain.Employees;
using HrManagement.Domain.Employees.Profiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

public sealed class EmployeeEmergencyContactConfiguration
    : IEntityTypeConfiguration<EmployeeEmergencyContact>
{
    public void Configure(
        EntityTypeBuilder<EmployeeEmergencyContact> builder)
    {
        builder.ToTable(
            "EmployeeEmergencyContacts");

        builder.HasKey(
            contact => contact.Id);

        builder.Property(
                contact => contact.Id)
            .ValueGeneratedNever();

        builder.Property(
                contact => contact.EmployeeId)
            .IsRequired();

        builder.Property(
                contact => contact.FullName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(
                contact => contact.Relationship)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(
                contact => contact.PhoneNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(
                contact => contact.Email)
            .HasMaxLength(256);

        builder.Property(
                contact => contact.IsPrimary)
            .IsRequired();

        builder.HasIndex(
        contact => new
            {
            contact.EmployeeId,
            contact.Id
            })
            .HasDatabaseName(
                "IX_EmployeeEmergencyContacts_EmployeeId_Id");

        builder.HasIndex(
                contact => contact.EmployeeId)
            .IsUnique()
            .HasFilter(
                "\"IsPrimary\" = 1")
            .HasDatabaseName(
                "UX_EmployeeEmergencyContacts_EmployeeId_Primary");

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(
                contact => contact.EmployeeId)
            .OnDelete(
                DeleteBehavior.Cascade);
    }
}
