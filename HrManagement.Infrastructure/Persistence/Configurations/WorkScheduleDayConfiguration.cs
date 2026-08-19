using HrManagement.Domain.Attendance.Schedules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

public sealed class WorkScheduleDayConfiguration
    : IEntityTypeConfiguration<WorkScheduleDay>
{
    public void Configure(
        EntityTypeBuilder<WorkScheduleDay> builder)
    {
        builder.ToTable(
            "WorkScheduleDays");

        builder.HasKey(
            day => day.Id);

        builder.Property(
                day => day.Id)
            .ValueGeneratedNever();

        builder.Property(
                day => day.WorkScheduleId)
            .IsRequired();

        builder.Property(
                day => day.DayOfWeek)
            .IsRequired();

        builder.Property(
                day => day.IsWorkingDay)
            .IsRequired();

        builder.Property(
            day => day.StartTime);

        builder.Property(
            day => day.EndTime);

        builder.Property(
                day => day.BreakMinutes)
            .IsRequired();

        builder.Ignore(
            day => day.PlannedMinutes);

        builder.Ignore(
            day => day.IsOvernight);

        builder.HasIndex(
                day => new
                {
                    day.WorkScheduleId,
                    day.DayOfWeek
                })
            .IsUnique()
            .HasDatabaseName(
                "UX_WorkScheduleDays_Schedule_DayOfWeek");

        builder.HasOne<WorkSchedule>()
            .WithMany()
            .HasForeignKey(
                day => day.WorkScheduleId)
            .OnDelete(
                DeleteBehavior.Cascade);
    }
}
