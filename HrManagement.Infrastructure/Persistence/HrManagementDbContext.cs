using HrManagement.Domain.Employees;
using Microsoft.EntityFrameworkCore;
using HrManagement.Infrastructure.Persistence.Configurations;
using HrManagement.Domain.Organization.Departments;
using HrManagement.Domain.Organization.Positions;
using HrManagement.Domain.Employees.OrganizationAssignments;
using HrManagement.Domain.Employees.Profiles;
using HrManagement.Domain.Attendance.Schedules;
using HrManagement.Domain.Auditing;
using HrManagement.Domain.Attendance.Records;
using HrManagement.Domain.Leave.Requests;
using HrManagement.Domain.Leave.Types;
using HrManagement.Domain.Attendance.Calendars;
using HrManagement.Domain.Attendance.Corrections;
using HrManagement.Domain.Attendance.Timesheets;
using HrManagement.Domain.Overtime.Requests;
using HrManagement.Domain.Payroll.Compensation;
using HrManagement.Domain.Payroll.Periods;
using HrManagement.Domain.Payroll.Snapshots;

namespace HrManagement.Infrastructure.Persistence;

public sealed class HrManagementDbContext : DbContext
{
    public DbSet<EmployeeCompensation>
        EmployeeCompensations =>
            Set<EmployeeCompensation>();

    public DbSet<PayrollPeriod>
        PayrollPeriods =>
            Set<PayrollPeriod>();

    public DbSet<PayrollEmployeeSnapshot>
        PayrollEmployeeSnapshots =>
            Set<PayrollEmployeeSnapshot>();

    public DbSet<OvertimeRequest>
        OvertimeRequests =>
            Set<OvertimeRequest>();

    public DbSet<OvertimeRequestStatusChange>
        OvertimeRequestStatusChanges =>
            Set<OvertimeRequestStatusChange>();

    public DbSet<WorkScheduleDateOverride>
        WorkScheduleDateOverrides =>
            Set<WorkScheduleDateOverride>();

    public DbSet<LeaveRequestStatusChange>
        LeaveRequestStatusChanges =>
            Set<LeaveRequestStatusChange>();

    public DbSet<LeaveType> LeaveTypes =>
        Set<LeaveType>();

    public DbSet<LeaveRequest> LeaveRequests =>
        Set<LeaveRequest>();

    public DbSet<HolidayCalendarDay> HolidayCalendarDays =>
        Set<HolidayCalendarDay>();

    public DbSet<TimesheetPeriod>
        TimesheetPeriods =>
            Set<TimesheetPeriod>();

    public DbSet<MonthlyTimesheetDaySnapshot>
        MonthlyTimesheetDaySnapshots =>
            Set<MonthlyTimesheetDaySnapshot>();

    public DbSet<AttendanceCorrection>
        AttendanceCorrections =>
            Set<AttendanceCorrection>();

    public DbSet<AttendanceRecord> AttendanceRecords =>
        Set<AttendanceRecord>();

    public DbSet<AttendanceEvent> AttendanceEvents =>
        Set<AttendanceEvent>();

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
            new HolidayCalendarDayConfiguration());

        modelBuilder.ApplyConfiguration(
            new WorkScheduleConfiguration());

        modelBuilder.ApplyConfiguration(
            new WorkScheduleDayConfiguration());

        modelBuilder.ApplyConfiguration(
            new WorkScheduleDateOverrideConfiguration());

        modelBuilder.ApplyConfiguration(
            new EmployeeWorkScheduleAssignmentConfiguration());

        modelBuilder.ApplyConfiguration(
            new AttendanceRecordConfiguration());

        modelBuilder.ApplyConfiguration(
            new AttendanceEventConfiguration());

        modelBuilder.ApplyConfiguration(
            new AttendanceCorrectionConfiguration());

        modelBuilder.ApplyConfiguration(
            new TimesheetPeriodConfiguration());

        modelBuilder.ApplyConfiguration(
            new MonthlyTimesheetDaySnapshotConfiguration());

        modelBuilder.ApplyConfiguration(
            new OvertimeRequestConfiguration());

        modelBuilder.ApplyConfiguration(
            new OvertimeRequestStatusChangeConfiguration());

        modelBuilder.ApplyConfiguration(
            new EmployeeCompensationConfiguration());

        modelBuilder.ApplyConfiguration(
            new PayrollPeriodConfiguration());

        modelBuilder.ApplyConfiguration(
            new PayrollEmployeeSnapshotConfiguration());

        modelBuilder.ApplyConfiguration(
            new LeaveTypeConfiguration());

        modelBuilder.ApplyConfiguration(
            new LeaveRequestConfiguration());

        modelBuilder.ApplyConfiguration(
            new LeaveRequestStatusChangeConfiguration());
    }
}
