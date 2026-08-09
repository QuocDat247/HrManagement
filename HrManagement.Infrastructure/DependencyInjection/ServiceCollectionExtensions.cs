using HrManagement.Application.Dashboard;
using HrManagement.Infrastructure.Dashboard;
using Microsoft.Extensions.DependencyInjection;
using HrManagement.Application.Employees;
using HrManagement.Infrastructure.Employees;

namespace HrManagement.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services)
    {
        services.AddSingleton<IDashboardService, FakeDashboardService>();

        services.AddSingleton<IEmployeeRepository, FakeEmployeeRepository>();
        services.AddSingleton<IEmployeeService, EmployeeService>();

        return services;
    }
}
