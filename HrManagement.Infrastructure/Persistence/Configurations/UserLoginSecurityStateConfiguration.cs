using HrManagement.Domain.Authentication.Accounts;
using HrManagement.Domain.Authentication.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

public sealed class UserLoginSecurityStateConfiguration
    : IEntityTypeConfiguration<UserLoginSecurityState>
{
    public void Configure(
        EntityTypeBuilder<UserLoginSecurityState> builder)
    {
        builder.ToTable(
            "UserLoginSecurityStates");

        builder.HasKey(state =>
            state.AccountId);

        builder.Property(state =>
                state.AccountId)
            .ValueGeneratedNever();

        builder.Property(state =>
                state.FailedLoginCount)
            .IsRequired();

        builder.Property(state =>
                state.LockoutEndUtc);

        builder.HasOne<UserAccount>()
            .WithOne()
            .HasForeignKey<UserLoginSecurityState>(
                state =>
                    state.AccountId)
            .OnDelete(
                DeleteBehavior.Cascade);
    }
}
