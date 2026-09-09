using HrManagement.Application.Authorization;
using HrManagement.Application.Authorization.Roles;
using HrManagement.Infrastructure.Authorization.Roles;
using HrManagement.Application.Authentication.Bootstrap;
using HrManagement.Infrastructure.Authentication.Bootstrap;
using HrManagement.Application.Authentication.Security;
using HrManagement.Infrastructure.Authentication.Security;
using HrManagement.Application.Authentication.Credentials;
using HrManagement.Infrastructure.Authentication.Credentials;
using HrManagement.Application.Payroll.Periods;
using HrManagement.Infrastructure.Payroll.Periods;
using HrManagement.Application.Payroll.Calculations;
using HrManagement.Infrastructure.Payroll.Calculations;
using HrManagement.Application.Payroll.Compensation;
using HrManagement.Infrastructure.Payroll.Compensation;
using HrManagement.Application.Attendance.Corrections;
using HrManagement.Infrastructure.Attendance.Corrections;
using HrManagement.Application.Workspaces.HolidayExceptions;
using HrManagement.Application.Overtime.Requests;
using HrManagement.Infrastructure.Overtime.Requests;
using HrManagement.Infrastructure.Workspaces.HolidayExceptions;
using HrManagement.Application.Attendance.Calculations;
using HrManagement.Application.Attendance.Calendars;
using HrManagement.Application.Attendance.Expectations;
using HrManagement.Application.Attendance.Generation;
using HrManagement.Application.Attendance.Records;
using HrManagement.Application.Attendance.Schedules;
using HrManagement.Application.Attendance.Timesheets;
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
using HrManagement.Application.Workspaces.Overtime;
using HrManagement.Domain.Attendance.Calculations;
using HrManagement.Infrastructure.Attendance.Calculations;
using HrManagement.Infrastructure.Attendance.Calendars;
using HrManagement.Infrastructure.Attendance.Expectations;
using HrManagement.Infrastructure.Attendance.Generation;
using HrManagement.Infrastructure.Attendance.Records;
using HrManagement.Infrastructure.Attendance.Schedules;
using HrManagement.Infrastructure.Attendance.Timesheets;
using HrManagement.Infrastructure.Attendance.Schedules.Overrides;
using HrManagement.Infrastructure.Dashboard;
using HrManagement.Infrastructure.Dashboard.Analytics;
using HrManagement.Application.Authentication.Accounts;
using HrManagement.Infrastructure.Authentication.Accounts;
using HrManagement.Infrastructure.Employees;
using HrManagement.Infrastructure.Employees.Profiles;
using HrManagement.Infrastructure.Leave.Requests;
using HrManagement.Infrastructure.Leave.Types;
using HrManagement.Infrastructure.Organization.Memberships;
using HrManagement.Infrastructure.Persistence;
using HrManagement.Infrastructure.Workspaces.AttendanceLeave;
using HrManagement.Infrastructure.Workspaces.WorkSchedules;
using HrManagement.Infrastructure.Workspaces.Overtime;
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

        services.AddScoped<
            IUserAccountRepository,
            EfUserAccountRepository>();

        services.AddScoped<
            IUserCredentialRepository,
            EfUserCredentialRepository>();

        services.AddScoped<
            IUserLoginSecurityStateRepository,
            EfUserLoginSecurityStateRepository>();

        services.AddScoped<
            IRoleRepository,
            EfRoleRepository>();

        services.AddScoped<
            IRoleManagementPersistence,
            EfRoleManagementPersistence>();

        services.AddScoped<
            RoleManagementService>();

        services.AddScoped<
            IRoleManagementService>(
                provider =>
                    new AuthorizedRoleManagementService(
                        provider.GetRequiredService<
                            RoleManagementService>(),
                        provider.GetRequiredService<
                            IAuthorizationGuard>()));

        services.AddScoped<
            IRoleActiveStatePersistence,
            EfRoleActiveStatePersistence>();

        services.AddScoped<
            RoleActiveStateService>();

        services.AddScoped<
            IRoleActiveStateService>(
                provider =>
                    new AuthorizedRoleActiveStateService(
                        provider.GetRequiredService<
                            RoleActiveStateService>(),
                        provider.GetRequiredService<
                            IAuthorizationGuard>()));

        services.AddScoped<
            IRolePermissionAssignmentPersistence,
            EfRolePermissionAssignmentPersistence>();

        services.AddScoped<
            RolePermissionAssignmentService>();

        services.AddScoped<
            IRolePermissionAssignmentService>(
                provider =>
                    new AuthorizedRolePermissionAssignmentService(
                        provider.GetRequiredService<
                            RolePermissionAssignmentService>(),
                        provider.GetRequiredService<
                            IAuthorizationGuard>()));

        services.AddScoped<
            IRolePermissionRepository,
            EfRolePermissionRepository>();

        services.AddScoped<
            IUserAccountRoleRepository,
            EfUserAccountRoleRepository>();

        services.AddSingleton<
            EfAccountManagementQueryService>();

        services.AddScoped<
            IAccountManagementQueryService>(
                provider =>
                    new AuthorizedAccountManagementQueryService(
                        provider.GetRequiredService<
                            EfAccountManagementQueryService>(),
                        provider.GetRequiredService<
                            IAuthorizationGuard>()));

        services.AddScoped<
            IStandardAccountCreationPersistence,
            EfStandardAccountCreationPersistence>();

        services.AddScoped<
            StandardAccountCreationService>();

        services.AddScoped<
            IStandardAccountCreationService>(
                provider =>
                    new AuthorizedStandardAccountCreationService(
                        provider.GetRequiredService<
                            StandardAccountCreationService>(),
                        provider.GetRequiredService<
                            IAuthorizationGuard>()));

        services.AddScoped<
            IAccountProfileUpdatePersistence,
            EfAccountProfileUpdatePersistence>();

        services.AddScoped<
            AccountProfileUpdateService>();

        services.AddScoped<
            IAccountProfileUpdateService>(
                provider =>
                    new AuthorizedAccountProfileUpdateService(
                        provider.GetRequiredService<
                            AccountProfileUpdateService>(),
                        provider.GetRequiredService<
                            IAuthorizationGuard>()));

        services.AddScoped<
            IAccountActiveStatePersistence,
            EfAccountActiveStatePersistence>();

        services.AddScoped<
            AccountActiveStateService>();

        services.AddScoped<
            IAccountActiveStateService>(
                provider =>
                    new AuthorizedAccountActiveStateService(
                        provider.GetRequiredService<
                            AccountActiveStateService>(),
                        provider.GetRequiredService<
                            IAuthorizationGuard>()));

        services.AddScoped<
            IAuthorizationService,
            AuthorizationService>();

        services.AddScoped<
            IAuthorizationGuard,
            AuthorizationGuard>();

        services.AddScoped<
            IInitialOwnerBootstrapPersistence,
            EfInitialOwnerBootstrapPersistence>();

        services.AddScoped<
            IInitialOwnerBootstrapService,
            InitialOwnerBootstrapService>();

        services.AddSingleton<
            IPasswordHasher,
            Pbkdf2PasswordHasher>();

        services.AddSingleton<
            IPasswordBlocklist,
            BundledPasswordBlocklist>();

        services.AddSingleton<
            IPasswordPolicy,
            DefaultPasswordPolicy>();

        services.AddScoped<IEmployeeRepository, EfEmployeeRepository>();

        services.AddScoped<
            EmployeeService>();

        services.AddScoped<
            IEmployeeService>(
                provider =>
                    new AuthorizedEmployeeService(
                        provider.GetRequiredService<
                            EmployeeService>(),
                        provider.GetRequiredService<
                            IAuthorizationGuard>()));

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
            HolidayCalendarManagementService>();

        services.AddScoped<
            IHolidayCalendarManagementService>(
                provider =>
                    new AuthorizedHolidayCalendarManagementService(
                        provider.GetRequiredService<
                            HolidayCalendarManagementService>(),
                        provider.GetRequiredService<
                            IAuthorizationGuard>()));

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
            WorkScheduleManagementService>();

        services.AddScoped<
            IWorkScheduleManagementService>(
                provider =>
                    new AuthorizedWorkScheduleManagementService(
                        provider.GetRequiredService<
                            WorkScheduleManagementService>(),
                        provider.GetRequiredService<
                            IAuthorizationGuard>()));

        services.AddSingleton<
            IEmployeeWorkScheduleAssignmentRepository,
            EfEmployeeWorkScheduleAssignmentRepository>();

        services.AddSingleton<
            IEmployeeWorkScheduleAssignmentPersistence,
            EfEmployeeWorkScheduleAssignmentPersistence>();

        services.AddScoped<
            EmployeeWorkScheduleAssignmentService>();

        services.AddScoped<
            IEmployeeWorkScheduleAssignmentService>(
                provider =>
                    new AuthorizedEmployeeWorkScheduleAssignmentService(
                        provider.GetRequiredService<
                            EmployeeWorkScheduleAssignmentService>(),
                        provider.GetRequiredService<
                            IAuthorizationGuard>()));

        services.AddSingleton<WorkScheduleSeedService>();

        services.AddSingleton<
            IMonthlyTimesheetQuerySource,
            EfMonthlyTimesheetQuerySource>();

        services.AddSingleton<
            ICloseTimesheetPeriodPersistence,
            EfCloseTimesheetPeriodPersistence>();

        services.AddSingleton<
            IAttendancePeriodLockPolicy,
            EfAttendancePeriodLockPolicy>();

        services.AddSingleton<
            IOvertimeRequestSubmissionContextSource,
            EfOvertimeRequestSubmissionContextSource>();

        services.AddSingleton<
            IOvertimeRequestSubmissionPersistence,
            EfOvertimeRequestSubmissionPersistence>();

        services.AddScoped<
            IOvertimeRequestSubmissionAuthorizationPolicy,
            PermissionOvertimeRequestSubmissionAuthorizationPolicy>();

        services.AddSingleton<
            EfOvertimeWorkspaceQueryService>();

        services.AddScoped<
            IOvertimeWorkspaceQueryService>(
                provider =>
                    new AuthorizedOvertimeWorkspaceQueryService(
                        provider.GetRequiredService<
                            EfOvertimeWorkspaceQueryService>(),
                        provider.GetRequiredService<
                            IAuthorizationGuard>()));

        services.AddScoped<
            ISubmitOvertimeRequestService,
            SubmitOvertimeRequestService>();

        services.AddSingleton<
            IOvertimeRequestStatusTransitionContextSource,
            EfOvertimeRequestStatusTransitionContextSource>();

        services.AddSingleton<
            IOvertimeRequestStatusTransitionPersistence,
            EfOvertimeRequestStatusTransitionPersistence>();

        services.AddScoped<
            IOvertimeRequestStatusAuthorizationPolicy,
            PermissionOvertimeRequestStatusAuthorizationPolicy>();

        services.AddScoped<
            IOvertimeRequestStatusService,
            OvertimeRequestStatusService>();

        services.AddSingleton<
            IEmployeeCompensationContextSource,
            EfEmployeeCompensationContextSource>();

        services.AddSingleton<
            IEmployeeCompensationPersistence,
            EfEmployeeCompensationPersistence>();

        services.AddSingleton<
            IEmployeeCompensationQuerySource,
            EfEmployeeCompensationQuerySource>();

        services.AddScoped<
            IEmployeeCompensationAuthorizationPolicy,
            PermissionEmployeeCompensationAuthorizationPolicy>();

        services.AddScoped<
            IEmployeeCompensationService,
            EmployeeCompensationService>();

        services.AddSingleton<
            IApprovedOvertimePayrollSource,
            EfApprovedOvertimePayrollSource>();

        services.AddSingleton<
            IBaseSalaryProrationPolicy,
            CalendarDayBaseSalaryProrationPolicy>();

        services.AddSingleton<
            IOvertimePayabilityPolicy,
            ConservativeOvertimePayabilityPolicy>();

        services.AddSingleton<
            IEmployeeOvertimePayabilityResolver,
            EmployeeOvertimePayabilityResolver>();

        services.AddSingleton(
            new PayrollPolicyProfile(
                standardMonthlyWorkingMinutes:
                    12_480,
                nonWorkingDayOvertimeMultiplier:
                    2.0m));

        services.AddSingleton<
            IPayrollPolicyProfileSource,
            ConfiguredPayrollPolicyProfileSource>();

        services.AddSingleton<
            IOvertimeAmountPolicy,
            ConfiguredOvertimeAmountPolicy>();

        services.AddSingleton<
            IMoneyRoundingPolicy,
            CurrencyMoneyRoundingPolicy>();

        services.AddScoped<
            PayrollPreviewService>();

        services.AddScoped<
            IPayrollPreviewService>(
                provider =>
                    new AuthorizedPayrollPreviewService(
                        provider.GetRequiredService<
                            PayrollPreviewService>(),
                        provider.GetRequiredService<
                            IAuthorizationGuard>()));

        services.AddSingleton<
            IClosePayrollPeriodPersistence,
            EfClosePayrollPeriodPersistence>();

        services.AddScoped<
            IPayrollPeriodClosingAuthorizationPolicy,
            PermissionPayrollPeriodClosingAuthorizationPolicy>();

        services.AddScoped<
            IClosePayrollPeriodService>(
                provider =>
                    ActivatorUtilities
                        .CreateInstance<
                            ClosePayrollPeriodService>(
                                provider,
                                provider.GetRequiredService<
                                    PayrollPreviewService>()));

        services.AddSingleton<
            EfClosedPayrollQueryService>();

        services.AddScoped<
            IClosedPayrollQueryService>(
                provider =>
                    new AuthorizedClosedPayrollQueryService(
                        provider.GetRequiredService<
                            EfClosedPayrollQueryService>(),
                        provider.GetRequiredService<
                            IAuthorizationGuard>()));

        services.AddSingleton<
            IPayrollFinancialPeriodLockSource,
            EfPayrollFinancialPeriodLockSource>();

        services.AddScoped<
            IPayrollCalculationInputService,
            PayrollCalculationInputService>();

        services.AddScoped<
            ITimesheetPeriodClosingAuthorizationPolicy,
            PermissionTimesheetPeriodClosingAuthorizationPolicy>();

        services.AddScoped<
            ICloseTimesheetPeriodService,
            CloseTimesheetPeriodService>();

        services.AddScoped<
            MonthlyTimesheetQueryService>();

        services.AddScoped<
            IMonthlyTimesheetQueryService>(
                provider =>
                    new AuthorizedMonthlyTimesheetQueryService(
                        provider.GetRequiredService<
                            MonthlyTimesheetQueryService>(),
                        provider.GetRequiredService<
                            IAuthorizationGuard>()));

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
            AttendancePunchService>();

        services.AddScoped<
            IAttendancePunchService>(
                provider =>
                    new AuthorizedAttendancePunchService(
                        provider.GetRequiredService<
                            AttendancePunchService>(),
                        provider.GetRequiredService<
                            IAuthorizationGuard>()));

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
            AttendanceRecalculationService>();

        services.AddScoped<
            IAttendanceRecalculationService>(
                provider =>
                    new AuthorizedAttendanceRecalculationService(
                        provider.GetRequiredService<
                            AttendanceRecalculationService>(),
                        provider.GetRequiredService<
                            IAuthorizationGuard>()));

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
            LeaveRequestSubmissionService>();

        services.AddScoped<
            ILeaveRequestSubmissionService>(
                provider =>
                    new AuthorizedLeaveRequestSubmissionService(
                        provider.GetRequiredService<
                            LeaveRequestSubmissionService>(),
                        provider.GetRequiredService<
                            IAuthorizationGuard>()));

        services.AddSingleton<LeaveTypeSeedService>();

        services.AddSingleton<
            ILeaveRequestStatusHistoryRepository,
            EfLeaveRequestStatusHistoryRepository>();

        services.AddSingleton<
            ILeaveRequestStatusTransitionPersistence,
            EfLeaveRequestStatusTransitionPersistence>();

        services.AddScoped<
            LeaveRequestStatusService>();

        services.AddScoped<
            ILeaveRequestStatusService>(
                provider =>
                    new AuthorizedLeaveRequestStatusService(
                        provider.GetRequiredService<
                            LeaveRequestStatusService>(),
                        provider.GetRequiredService<
                            IAuthorizationGuard>()));

        services.AddSingleton<
            EfAttendanceLeaveWorkspaceQueryService>();

        services.AddScoped<
            IAttendanceLeaveWorkspaceQueryService>(
                provider =>
                    new AuthorizedAttendanceLeaveWorkspaceQueryService(
                        provider.GetRequiredService<
                            EfAttendanceLeaveWorkspaceQueryService>(),
                        provider.GetRequiredService<
                            IAuthorizationGuard>()));

        services.AddSingleton<
            EfWorkScheduleWorkspaceQueryService>();

        services.AddScoped<
            IWorkScheduleWorkspaceQueryService>(
                provider =>
                    new AuthorizedWorkScheduleWorkspaceQueryService(
                        provider.GetRequiredService<
                            EfWorkScheduleWorkspaceQueryService>(),
                        provider.GetRequiredService<
                            IAuthorizationGuard>()));

        services.AddSingleton<
            IWorkScheduleDayManagementPersistence,
            EfWorkScheduleDayManagementPersistence>();

        services.AddScoped<
            WorkScheduleDayManagementService>();

        services.AddScoped<
            IWorkScheduleDayManagementService>(
                provider =>
                    new AuthorizedWorkScheduleDayManagementService(
                        provider.GetRequiredService<
                            WorkScheduleDayManagementService>(),
                        provider.GetRequiredService<
                            IAuthorizationGuard>()));

        services.AddScoped<
            DailyAttendanceGenerationService>();

        services.AddScoped<
            IDailyAttendanceGenerationService>(
                provider =>
                    new AuthorizedDailyAttendanceGenerationService(
                        provider.GetRequiredService<
                            DailyAttendanceGenerationService>(),
                        provider.GetRequiredService<
                            IAuthorizationGuard>()));

        services.AddSingleton<
            IDailyAttendanceGenerationPersistence,
            EfDailyAttendanceGenerationPersistence>();

        services.AddSingleton<
            IEffectiveAttendanceTimelineResolver,
            EffectiveAttendanceTimelineResolver>();

        services.AddSingleton<
            IAttendanceCorrectionPersistence,
            EfAttendanceCorrectionPersistence>();

        services.AddScoped<
            IAttendanceCorrectionAuthorizationPolicy,
            PermissionAttendanceCorrectionAuthorizationPolicy>();

        services.AddScoped<
            IAttendanceCorrectionService,
            AttendanceCorrectionService>();

        services.AddScoped<
            AttendanceCorrectionWorkspaceQueryService>();

        services.AddScoped<
            IAttendanceCorrectionWorkspaceQueryService>(
                provider =>
                    new AuthorizedAttendanceCorrectionWorkspaceQueryService(
                        provider.GetRequiredService<
                            AttendanceCorrectionWorkspaceQueryService>(),
                        provider.GetRequiredService<
                            IAuthorizationGuard>()));

        services.AddSingleton<
            EfHolidayExceptionWorkspaceQueryService>();

        services.AddScoped<
            IHolidayExceptionWorkspaceQueryService>(
                provider =>
                    new AuthorizedHolidayExceptionWorkspaceQueryService(
                        provider.GetRequiredService<
                            EfHolidayExceptionWorkspaceQueryService>(),
                        provider.GetRequiredService<
                            IAuthorizationGuard>()));

        return services;
    }
}
