using HrManagement.Domain.Attendance.Calendars;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

public sealed class HolidayCalendarDayConfiguration
    : IEntityTypeConfiguration<HolidayCalendarDay>
{
    public void Configure(
        EntityTypeBuilder<HolidayCalendarDay> builder)
    {
        builder.ToTable(
            "HolidayCalendarDays");

        builder.HasKey(
            holiday => holiday.Id);

        builder.Property(
                holiday => holiday.Id)
            .ValueGeneratedNever();

        builder.Property(
                holiday => holiday.Date)
            .IsRequired();

        builder.Property(
                holiday => holiday.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(
                holiday => holiday.IsActive)
            .IsRequired();

        builder.HasIndex(
                holiday => holiday.Date)
            .IsUnique()
            .HasDatabaseName(
                "UX_HolidayCalendarDays_Date");

        builder.HasIndex(
            holiday => holiday.IsActive);
    }
}
