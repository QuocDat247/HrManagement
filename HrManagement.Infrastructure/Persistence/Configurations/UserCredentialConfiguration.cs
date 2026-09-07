using HrManagement.Domain.Authentication.Accounts;
using HrManagement.Domain.Authentication.Credentials;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

public sealed class UserCredentialConfiguration
    : IEntityTypeConfiguration<UserCredential>
{
    public void Configure(
        EntityTypeBuilder<UserCredential> builder)
    {
        builder.ToTable(
            "UserCredentials");

        builder.HasKey(credential =>
            credential.AccountId);

        builder.Property(credential =>
                credential.AccountId)
            .ValueGeneratedNever();

        builder.Property(credential =>
                credential.PasswordHash)
            .HasMaxLength(1024)
            .IsRequired();

        builder.Property(credential =>
                credential.MustChangePassword)
            .IsRequired();

        builder.HasOne<UserAccount>()
            .WithOne()
            .HasForeignKey<UserCredential>(
                credential =>
                    credential.AccountId)
            .OnDelete(
                DeleteBehavior.Cascade);
    }
}
