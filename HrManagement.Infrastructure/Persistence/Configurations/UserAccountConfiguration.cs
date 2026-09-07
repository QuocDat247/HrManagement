using HrManagement.Domain.Authentication.Accounts;
using HrManagement.Domain.Employees;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

public sealed class UserAccountConfiguration
    : IEntityTypeConfiguration<UserAccount>
{
    public void Configure(
        EntityTypeBuilder<UserAccount> builder)
    {
        builder.ToTable(
            "UserAccounts");

        builder.HasKey(account =>
            account.Id);

        builder.Property(account =>
                account.Id)
            .ValueGeneratedNever();

        builder.Property(account =>
                account.Username)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(account =>
                account.NormalizedUsername)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(account =>
                account.DisplayName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(account =>
                account.EmployeeId);

        builder.Property(account =>
                account.Kind)
            .IsRequired();

        builder.Property(account =>
                account.IsActive)
            .IsRequired();

        builder.HasIndex(account =>
                account.NormalizedUsername)
            .IsUnique()
            .HasDatabaseName(
                "UX_UserAccounts_NormalizedUsername");

        builder.HasIndex(account =>
                account.EmployeeId)
            .IsUnique()
            .HasDatabaseName(
                "UX_UserAccounts_EmployeeId");

        builder.HasOne<Employee>()
            .WithOne()
            .HasForeignKey<UserAccount>(
                account =>
                    account.EmployeeId)
            .OnDelete(
                DeleteBehavior.Restrict);
    }
}
