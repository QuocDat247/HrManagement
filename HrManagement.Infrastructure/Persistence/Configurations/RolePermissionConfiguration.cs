using HrManagement.Domain.Authorization.Roles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

public sealed class RolePermissionConfiguration
    : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(
        EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable(
            "RolePermissions");

        builder.HasKey(
            rolePermission =>
                new
                {
                    rolePermission.RoleId,
                    rolePermission.PermissionCode
                });

        builder.Property(
                rolePermission =>
                    rolePermission.RoleId)
            .IsRequired();

        builder.Property(
                rolePermission =>
                    rolePermission.PermissionCode)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasOne<Role>()
            .WithMany()
            .HasForeignKey(
                rolePermission =>
                    rolePermission.RoleId)
            .OnDelete(
                DeleteBehavior.Cascade);

        builder.HasIndex(
                rolePermission =>
                    rolePermission.PermissionCode)
            .HasDatabaseName(
                "IX_RolePermissions_PermissionCode");
    }
}
