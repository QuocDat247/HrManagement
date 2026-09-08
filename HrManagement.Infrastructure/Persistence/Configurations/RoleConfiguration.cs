using HrManagement.Domain.Authorization.Roles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

public sealed class RoleConfiguration
    : IEntityTypeConfiguration<Role>
{
    public void Configure(
        EntityTypeBuilder<Role> builder)
    {
        builder.ToTable(
            "Roles");

        builder.HasKey(
            role => role.Id);

        builder.Property(
                role => role.Id)
            .ValueGeneratedNever();

        builder.Property(
                role => role.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(
                role => role.NormalizedName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(
                role => role.Description)
            .HasMaxLength(500);

        builder.Property(
                role => role.IsActive)
            .IsRequired();

        builder.HasIndex(
                role => role.NormalizedName)
            .IsUnique()
            .HasDatabaseName(
                "UX_Roles_NormalizedName");
    }
}
