using HrManagement.Domain.Employees;
using Microsoft.EntityFrameworkCore;
using HrManagement.Infrastructure.Persistence.Configurations;
using HrManagement.Domain.Organization.Departments;
using HrManagement.Domain.Organization.Positions;
using HrManagement.Domain.Employees.OrganizationAssignments;
using HrManagement.Domain.Employees.Profiles;
using HrManagement.Domain.Attendance.Schedules;
using HrManagement.Domain.Auditing;

namespace HrManagement.Infrastructure.Persistence;

public sealed class HrManagementDbContext : DbContext
{
    public DbSet<WorkSchedule> WorkSchedules =>
        Set<WorkSchedule>();

    public DbSet<WorkScheduleDay> WorkScheduleDays =>
        Set<WorkScheduleDay>();

    public DbSet<EmployeeWorkScheduleAssignment>
        EmployeeWorkScheduleAssignments =>
            Set<EmployeeWorkScheduleAssignment>();

    public DbSet<AuditEntry> AuditEntries =>
        Set<AuditEntry>();

    public DbSet<EmployeeIdentificationRecord>
        EmployeeIdentificationRecords =>
            Set<EmployeeIdentificationRecord>();

    public DbSet<EmployeeEmergencyContact>
        EmployeeEmergencyContacts =>
            Set<EmployeeEmergencyContact>();

    public DbSet<EmployeeAddress>
        EmployeeAddresses =>
            Set<EmployeeAddress>();

    public DbSet<EmployeePersonalProfile>
        EmployeePersonalProfiles =>
            Set<EmployeePersonalProfile>();

    public DbSet<EmployeeOrganizationAssignment>
        EmployeeOrganizationAssignments =>
            Set<EmployeeOrganizationAssignment>();

    public DbSet<Position> Positions =>
        Set<Position>();

    public DbSet<EmploymentPeriod> EmploymentPeriods =>
        Set<EmploymentPeriod>();

    public DbSet<Department> Departments =>
        Set<Department>();


    public HrManagementDbContext(
        DbContextOptions<HrManagementDbContext> options)
        : base(options)
    {
    }

    public DbSet<Employee> Employees => Set<Employee>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(
            new EmployeeConfiguration());

        modelBuilder.ApplyConfiguration(
            new EmploymentPeriodConfiguration());

        modelBuilder.ApplyConfiguration(
            new DepartmentConfiguration());

        modelBuilder.ApplyConfiguration(
            new PositionConfiguration());

        modelBuilder.ApplyConfiguration(
            new EmployeeOrganizationAssignmentConfiguration());

        modelBuilder.ApplyConfiguration(
            new EmployeePersonalProfileConfiguration());

        modelBuilder.ApplyConfiguration(
            new EmployeeAddressConfiguration());

        modelBuilder.ApplyConfiguration(
            new EmployeeEmergencyContactConfiguration());

        modelBuilder.ApplyConfiguration(
            new EmployeeIdentificationRecordConfiguration());

        modelBuilder.ApplyConfiguration(
            new AuditEntryConfiguration());

        modelBuilder.ApplyConfiguration(
            new WorkScheduleConfiguration());

        modelBuilder.ApplyConfiguration(
            new WorkScheduleDayConfiguration());

        modelBuilder.ApplyConfiguration(
            new EmployeeWorkScheduleAssignmentConfiguration());
    }
}
