using HrManagement.Application.Dashboard;
using HrManagement.Infrastructure.Dashboard;
using Microsoft.Extensions.DependencyInjection;

namespace HrManagement.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services)
    {
        services.AddSingleton<IDashboardService, FakeDashboardService>();

        return services;
    }
}
