using HrManagement.Domain.Authentication.Accounts;
using HrManagement.Domain.Authentication.Recovery;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

public sealed class OwnerRecoveryCredentialConfiguration
    : IEntityTypeConfiguration<OwnerRecoveryCredential>
{
    public void Configure(
        EntityTypeBuilder<OwnerRecoveryCredential> builder)
    {
        builder.ToTable(
            "OwnerRecoveryCredentials");

        builder.HasKey(
            credential =>
                credential.AccountId);

        builder.Property(
                credential =>
                    credential.AccountId)
            .ValueGeneratedNever();

        builder.Property(
                credential =>
                    credential.RecoveryCodeHash)
            .HasMaxLength(1024)
            .IsRequired();

        builder.HasOne<UserAccount>()
            .WithOne()
            .HasForeignKey<OwnerRecoveryCredential>(
                credential =>
                    credential.AccountId)
            .OnDelete(
                DeleteBehavior.Cascade);
    }
}
