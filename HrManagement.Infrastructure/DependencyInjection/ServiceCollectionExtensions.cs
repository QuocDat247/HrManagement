using HrManagement.Application.Workspaces.HolidayExceptions;
using HrManagement.Infrastructure.Workspaces.HolidayExceptions;
using HrManagement.Application.Attendance.Calculations;
using HrManagement.Application.Attendance.Calendars;
using HrManagement.Application.Attendance.Expectations;
using HrManagement.Application.Attendance.Generation;
using HrManagement.Application.Attendance.Records;
using HrManagement.Application.Attendance.Schedules;
using HrManagement.Application.Attendance.Schedules.Overrides;
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
using HrManagement.Application.Workspaces.AttendanceLeave;
using HrManagement.Application.Workspaces.WorkSchedules;
using HrManagement.Domain.Attendance.Calculations;
using HrManagement.Infrastructure.Attendance.Calculations;
using HrManagement.Infrastructure.Attendance.Calendars;
using HrManagement.Infrastructure.Attendance.Expectations;
using HrManagement.Infrastructure.Attendance.Generation;
using HrManagement.Infrastructure.Attendance.Records;
using HrManagement.Infrastructure.Attendance.Schedules;
using HrManagement.Infrastructure.Attendance.Schedules.Overrides;
using HrManagement.Infrastructure.Dashboard;
using HrManagement.Infrastructure.Dashboard.Analytics;
using HrManagement.Infrastructure.Employees;
using HrManagement.Infrastructure.Employees.Profiles;
using HrManagement.Infrastructure.Leave.Requests;
using HrManagement.Infrastructure.Leave.Types;
using HrManagement.Infrastructure.Organization.Memberships;
using HrManagement.Infrastructure.Persistence;
using HrManagement.Infrastructure.Workspaces.AttendanceLeave;
using HrManagement.Infrastructure.Workspaces.WorkSchedules;
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
            IHolidayCalendarManagementPersistence,
            EfHolidayCalendarManagementPersistence>();

        services.AddScoped<
            IHolidayCalendarManagementService,
            HolidayCalendarManagementService>();

        services.AddSingleton<
            IWorkScheduleRepository,
            EfWorkScheduleRepository>();

        services.AddSingleton<
            IWorkScheduleDateOverrideManagementPersistence,
            EfWorkScheduleDateOverrideManagementPersistence>();

        services.AddScoped<
            IWorkScheduleDateOverrideManagementService,
            WorkScheduleDateOverrideManagementService>();

        services.AddSingleton<
            IWorkScheduleManagementPersistence,
            EfWorkScheduleManagementPersistence>();

        services.AddScoped<
            IWorkScheduleManagementService,
            WorkScheduleManagementService>();

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

        services.AddSingleton<
            IApprovedLeaveAttendanceResolver,
            EfApprovedLeaveAttendanceResolver>();

        services.AddScoped<
            IAttendanceRecalculationService,
            AttendanceRecalculationService>();

        services.AddSingleton<
            IWorkExpectationResolutionPersistence,
            EfWorkExpectationResolutionPersistence>();

        services.AddScoped<
            IWorkExpectationResolver,
            WorkExpectationResolver>();

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
        services.AddSingleton<
            IAttendanceLeaveWorkspaceQueryService,
            EfAttendanceLeaveWorkspaceQueryService>();

        services.AddSingleton<
            IWorkScheduleWorkspaceQueryService,
            EfWorkScheduleWorkspaceQueryService>();

        services.AddSingleton<
            IWorkScheduleDayManagementPersistence,
            EfWorkScheduleDayManagementPersistence>();

        services.AddScoped<
            IWorkScheduleDayManagementService,
            WorkScheduleDayManagementService>();

        services.AddScoped<
            IDailyAttendanceGenerationService,
            DailyAttendanceGenerationService>();

        services.AddSingleton<
            IDailyAttendanceGenerationPersistence,
            EfDailyAttendanceGenerationPersistence>();

        services.AddScoped<
            IDailyAttendanceGenerationService,
            DailyAttendanceGenerationService>();

        services.AddSingleton<
            IHolidayExceptionWorkspaceQueryService,
            EfHolidayExceptionWorkspaceQueryService>();

        return services;
    }
}
