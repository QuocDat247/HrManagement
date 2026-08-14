using HrManagement.Domain.Organization.Departments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

public sealed class DepartmentConfiguration
    : IEntityTypeConfiguration<Department>
{
    public void Configure(
        EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("Departments");

        builder.HasKey(department =>
            department.Id);

        builder.Property(department =>
                department.Id)
            .ValueGeneratedNever();

        builder.Property(department =>
                department.Code)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(department =>
                department.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(department =>
                department.IsActive)
            .IsRequired();

        builder.HasIndex(department =>
                department.Code)
            .IsUnique()
            .HasDatabaseName(
                "UX_Departments_Code");
    }
}
