using HrManagement.Domain.Attendance.Schedules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

public sealed class WorkScheduleDateOverrideConfiguration
    : IEntityTypeConfiguration<WorkScheduleDateOverride>
{
    public void Configure(
        EntityTypeBuilder<WorkScheduleDateOverride> builder)
    {
        builder.ToTable(
            "WorkScheduleDateOverrides");

        builder.HasKey(
            item => item.Id);

        builder.Property(
                item => item.Id)
            .ValueGeneratedNever();

        builder.Property(
                item => item.WorkScheduleId)
            .IsRequired();

        builder.Property(
                item => item.WorkDate)
            .IsRequired();

        builder.Property(
                item => item.IsWorkingDay)
            .IsRequired();

        builder.Property(
            item => item.StartTime);

        builder.Property(
            item => item.EndTime);

        builder.Property(
                item => item.BreakMinutes)
            .IsRequired();

        builder.Property(
                item => item.Note)
            .HasMaxLength(
                500);

        builder.Ignore(
            item => item.PlannedMinutes);

        builder.Ignore(
            item => item.IsOvernight);

        builder.HasIndex(
                item => new
                {
                    item.WorkScheduleId,
                    item.WorkDate
                })
            .IsUnique()
            .HasDatabaseName(
                "UX_WorkScheduleDateOverrides_Schedule_Date");

        builder.HasIndex(
                item => item.WorkDate)
            .HasDatabaseName(
                "IX_WorkScheduleDateOverrides_WorkDate");

        builder.HasOne<WorkSchedule>()
            .WithMany()
            .HasForeignKey(
                item => item.WorkScheduleId)
            .OnDelete(
                DeleteBehavior.Cascade);
    }
}
