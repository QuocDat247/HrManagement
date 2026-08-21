using HrManagement.Application.Attendance.Calculations;
using HrManagement.Application.Attendance.Records;
using HrManagement.Application.Attendance.Schedules;
using HrManagement.Application.Dashboard;
using HrManagement.Application.Dashboard.Analytics;
using HrManagement.Application.Employees;
using HrManagement.Application.Employees.EmploymentHistories;
using HrManagement.Application.Employees.EmploymentLifecycle;
using HrManagement.Application.Employees.OrganizationAssignments;
using HrManagement.Application.Employees.Profiles;
using HrManagement.Application.Leave.Requests;
using HrManagement.Application.Leave.Types;
using HrManagement.Application.Organization.Memberships;
using HrManagement.Domain.Attendance.Calculations;
using HrManagement.Infrastructure.Attendance.Calculations;
using HrManagement.Infrastructure.Attendance.Records;
using HrManagement.Infrastructure.Attendance.Schedules;
using HrManagement.Infrastructure.Dashboard;
using HrManagement.Infrastructure.Dashboard.Analytics;
using HrManagement.Infrastructure.Employees;
using HrManagement.Infrastructure.Employees.Profiles;
using HrManagement.Infrastructure.Leave.Requests;
using HrManagement.Infrastructure.Leave.Types;
using HrManagement.Infrastructure.Organization.Memberships;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;


namespace HrManagement.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
    this IServiceCollection services)
    {
        services.AddDbContextFactory<HrManagementDbContext>(
            options =>
                options.UseSqlite(
                    DatabasePath.GetConnectionString()));

        services.AddSingleton<IDashboardService, EfDashboardService>();

        services.AddScoped<IEmployeeRepository, EfEmployeeRepository>();

        services.AddScoped<IEmployeeService, EmployeeService>();

        services.AddSingleton<DatabaseInitializer>();

        services.AddSingleton<IWorkforceAnalyticsService, EfWorkforceAnalyticsService>();

        services.AddSingleton<IEmploymentHistoryRepository, EfEmploymentHistoryRepository>();

        services.AddSingleton<
        IEmploymentHistoryBackfillService,
        EfEmploymentHistoryBackfillService>();

        services.AddSingleton<
        IEmploymentLifecyclePersistence,
        EfEmploymentLifecyclePersistence>();

        services.AddSingleton<
        IOrganizationMembershipQueryService,
        EfOrganizationMembershipQueryService>();

        services.AddSingleton<
        IEmployeeOrganizationHistoryRepository,
        EfEmployeeOrganizationHistoryRepository>();

        services.AddSingleton<
        IEmployeeOrganizationAssignmentBackfillService,
        EfEmployeeOrganizationAssignmentBackfillService>();

        services.AddScoped<
            IEmployeeOrganizationTransferService,
            EmployeeOrganizationTransferService>();

        services.AddSingleton<
            IEmployeeOrganizationTransferPersistence,
            EfEmployeeOrganizationTransferPersistence>();

        services.AddSingleton<
            IEmployeePersonalProfileRepository,
            EfEmployeePersonalProfileRepository>();

        services.AddScoped<
            IEmployeePersonalProfileService,
            EmployeePersonalProfileService>();

        services.AddSingleton<
            IEmployeeAddressRepository,
            EfEmployeeAddressRepository>();

        services.AddScoped<
            IEmployeeAddressService,
            EmployeeAddressService>();

        services.AddScoped<
            IEmployeeEmergencyContactService,
            EmployeeEmergencyContactService>();

        services.AddSingleton<
            IEmployeeIdentificationRecordRepository,
            EfEmployeeIdentificationRecordRepository>();

        services.AddScoped<
            IEmployeeIdentificationRecordService,
            EmployeeIdentificationRecordService>();

        services.AddSingleton<
            IWorkScheduleRepository,
            EfWorkScheduleRepository>();

        services.AddSingleton<
            IEmployeeWorkScheduleAssignmentRepository,
            EfEmployeeWorkScheduleAssignmentRepository>();

        services.AddSingleton<
            IEmployeeWorkScheduleAssignmentPersistence,
            EfEmployeeWorkScheduleAssignmentPersistence>();

        services.AddScoped<
            IEmployeeWorkScheduleAssignmentService,
            EmployeeWorkScheduleAssignmentService>();

        services.AddSingleton<WorkScheduleSeedService>();

        services.AddSingleton<
            IAttendanceRecordRepository,
            EfAttendanceRecordRepository>();

        services.AddSingleton<
            IAttendanceEventRepository,
            EfAttendanceEventRepository>();

        services.AddSingleton<
            IWorkScheduleDayRepository,
            EfWorkScheduleDayRepository>();

        services.AddSingleton<
            IAttendanceTimeZoneConverter,
            SystemAttendanceTimeZoneConverter>();

        services.AddSingleton<
            IAttendancePunchPersistence,
            EfAttendancePunchPersistence>();

        services.AddScoped<
            IAttendancePunchContextResolver,
            AttendancePunchContextResolver>();

        services.AddScoped<
            IAttendancePunchService,
            AttendancePunchService>();

        services.AddSingleton<
            IAttendanceScheduleWindowResolver,
            SystemAttendanceScheduleWindowResolver>();

        services.AddSingleton<
            IAttendanceCalculationPersistence,
            EfAttendanceCalculationPersistence>();

        services.AddSingleton(
            new AttendanceAdherencePolicy());

        services.AddScoped<
            IAttendanceRecalculationService,
            AttendanceRecalculationService>();

        services.AddSingleton<
            ILeaveTypeRepository,
            EfLeaveTypeRepository>();

        services.AddSingleton<
            ILeaveRequestRepository,
            EfLeaveRequestRepository>();

        services.AddSingleton<
            ILeaveRequestSubmissionPersistence,
            EfLeaveRequestSubmissionPersistence>();

        services.AddSingleton<TimeProvider>(
            TimeProvider.System);

        services.AddScoped<
            ILeaveRequestSubmissionService,
            LeaveRequestSubmissionService>();

        services.AddSingleton<LeaveTypeSeedService>();

        services.AddSingleton<
            ILeaveRequestStatusHistoryRepository,
            EfLeaveRequestStatusHistoryRepository>();

        services.AddSingleton<
            ILeaveRequestStatusTransitionPersistence,
            EfLeaveRequestStatusTransitionPersistence>();

        services.AddScoped<
            ILeaveRequestStatusService,
            LeaveRequestStatusService>();

        return services;
    }
}
