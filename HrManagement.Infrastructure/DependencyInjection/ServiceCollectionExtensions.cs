using HrManagement.Application.Dashboard;
using HrManagement.Application.Dashboard.Analytics;
using HrManagement.Application.Employees;
using HrManagement.Application.Employees.EmploymentHistories;
using HrManagement.Application.Employees.EmploymentLifecycle;
using HrManagement.Infrastructure.Dashboard;
using HrManagement.Infrastructure.Dashboard.Analytics;
using HrManagement.Infrastructure.Employees;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using HrManagement.Application.Organization.Memberships;
using HrManagement.Infrastructure.Organization.Memberships;
using HrManagement.Application.Employees.OrganizationAssignments;
using HrManagement.Application.Employees.Profiles;
using HrManagement.Infrastructure.Employees.Profiles;

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

        return services;
    }
}
