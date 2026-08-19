using HrManagement.Domain.Attendance.Schedules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

public sealed class WorkScheduleConfiguration
    : IEntityTypeConfiguration<WorkSchedule>
{
    public void Configure(
        EntityTypeBuilder<WorkSchedule> builder)
    {
        builder.ToTable(
            "WorkSchedules");

        builder.HasKey(
            schedule => schedule.Id);

        builder.Property(
                schedule => schedule.Id)
            .ValueGeneratedNever();

        builder.Property(
                schedule => schedule.Code)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(
                schedule => schedule.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(
                schedule => schedule.TimeZoneId)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(
                schedule => schedule.IsActive)
            .IsRequired();

        builder.HasIndex(
                schedule => schedule.Code)
            .IsUnique()
            .HasDatabaseName(
                "UX_WorkSchedules_Code");

        builder.HasIndex(
            schedule => schedule.IsActive);
    }
}
