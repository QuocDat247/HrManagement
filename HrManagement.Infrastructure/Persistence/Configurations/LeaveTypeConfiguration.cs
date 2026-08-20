using HrManagement.Domain.Leave.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

public sealed class LeaveTypeConfiguration
    : IEntityTypeConfiguration<LeaveType>
{
    public void Configure(
        EntityTypeBuilder<LeaveType> builder)
    {
        builder.ToTable(
            "LeaveTypes");

        builder.HasKey(
            leaveType => leaveType.Id);

        builder.Property(
                leaveType => leaveType.Id)
            .ValueGeneratedNever();

        builder.Property(
                leaveType => leaveType.Code)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(
                leaveType => leaveType.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(
                leaveType => leaveType.IsPaid)
            .IsRequired();

        builder.Property(
                leaveType => leaveType.IsActive)
            .IsRequired();

        builder.HasIndex(
                leaveType => leaveType.Code)
            .IsUnique()
            .HasDatabaseName(
                "UX_LeaveTypes_Code");

        builder.HasIndex(
                leaveType => leaveType.IsActive)
            .HasDatabaseName(
                "IX_LeaveTypes_IsActive");
    }
}
