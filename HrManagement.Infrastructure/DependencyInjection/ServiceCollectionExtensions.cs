using HrManagement.Application.Dashboard;
using HrManagement.Infrastructure.Dashboard;
using Microsoft.Extensions.DependencyInjection;
using HrManagement.Application.Employees;
using HrManagement.Infrastructure.Employees;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
    this IServiceCollection services)
    {
        services.AddDbContextFactory<HrManagementDbContext>(
            options =>
                options.UseSqlite(
                    "Data Source=hrmanagement.db"));

        services.AddSingleton<IDashboardService, EfDashboardService>();

        services.AddScoped<IEmployeeRepository, EfEmployeeRepository>();
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddSingleton<DatabaseInitializer>();

        return services;
    }
}
