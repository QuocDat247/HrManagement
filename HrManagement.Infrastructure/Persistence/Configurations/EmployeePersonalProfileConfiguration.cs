using HrManagement.Domain.Employees;
using HrManagement.Domain.Employees.Profiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

public sealed class EmployeePersonalProfileConfiguration
    : IEntityTypeConfiguration<EmployeePersonalProfile>
{
    public void Configure(
        EntityTypeBuilder<EmployeePersonalProfile> builder)
    {
        builder.ToTable(
            "EmployeePersonalProfiles");

        builder.HasKey(
            profile => profile.EmployeeId);

        builder.Property(
                profile => profile.EmployeeId)
            .ValueGeneratedNever();

        builder.Property(
                profile => profile.PreferredName)
            .HasMaxLength(150);

        builder.Property(
            profile => profile.Gender);

        builder.Property(
                profile => profile.Nationality)
            .HasMaxLength(100);

        builder.Property(
                profile => profile.PlaceOfBirth)
            .HasMaxLength(200);

        builder.HasOne<Employee>()
            .WithOne()
            .HasForeignKey<EmployeePersonalProfile>(
                profile => profile.EmployeeId)
            .OnDelete(
                DeleteBehavior.Cascade);
    }
}
