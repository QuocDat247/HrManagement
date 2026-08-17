using HrManagement.Domain.Employees;
using HrManagement.Domain.Employees.Profiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

public sealed class EmployeeIdentificationRecordConfiguration
    : IEntityTypeConfiguration<EmployeeIdentificationRecord>
{
    public void Configure(
        EntityTypeBuilder<EmployeeIdentificationRecord> builder)
    {
        builder.ToTable(
            "EmployeeIdentificationRecords");

        builder.HasKey(
            record => record.Id);

        builder.Property(
                record => record.Id)
            .ValueGeneratedNever();

        builder.Property(
                record => record.EmployeeId)
            .IsRequired();

        builder.Property(
                record => record.Type)
            .IsRequired();

        builder.Property(
                record => record.DocumentNumber)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(
            record => record.IssueDate);

        builder.Property(
            record => record.ExpiryDate);

        builder.Property(
                record => record.IssuingAuthority)
            .HasMaxLength(200);

        builder.Property(
                record => record.PlaceOfIssue)
            .HasMaxLength(200);

        builder.Property(
                record => record.IssuingCountry)
            .HasMaxLength(100);

        builder.HasIndex(
                record => new
                {
                    record.EmployeeId,
                    record.Type
                })
            .HasDatabaseName(
                "IX_EmployeeIdentificationRecords_EmployeeId_Type");

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(
                record => record.EmployeeId)
            .OnDelete(
                DeleteBehavior.Cascade);
    }
}
