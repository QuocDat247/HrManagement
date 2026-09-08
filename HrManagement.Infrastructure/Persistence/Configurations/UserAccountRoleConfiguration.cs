using HrManagement.Domain.Authentication.Accounts;
using HrManagement.Domain.Authorization.Roles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

public sealed class UserAccountRoleConfiguration
    : IEntityTypeConfiguration<UserAccountRole>
{
    public void Configure(
        EntityTypeBuilder<UserAccountRole> builder)
    {
        builder.ToTable(
            "UserAccountRoles");

        builder.HasKey(
            assignment =>
                new
                {
                    assignment.AccountId,
                    assignment.RoleId
                });

        builder.Property(
                assignment =>
                    assignment.AccountId)
            .IsRequired();

        builder.Property(
                assignment =>
                    assignment.RoleId)
            .IsRequired();

        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(
                assignment =>
                    assignment.AccountId)
            .OnDelete(
                DeleteBehavior.Cascade);

        builder.HasOne<Role>()
            .WithMany()
            .HasForeignKey(
                assignment =>
                    assignment.RoleId)
            .OnDelete(
                DeleteBehavior.Cascade);

        builder.HasIndex(
                assignment =>
                    assignment.RoleId)
            .HasDatabaseName(
                "IX_UserAccountRoles_RoleId");
    }
}
