using Microsoft.Extensions.DependencyInjection;

namespace Project.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Registered clean architecture Infrastructure services
        return services;
    }
}
