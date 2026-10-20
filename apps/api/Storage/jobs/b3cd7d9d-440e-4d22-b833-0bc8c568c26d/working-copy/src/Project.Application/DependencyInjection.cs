using Microsoft.Extensions.DependencyInjection;

namespace Project.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Registered clean architecture Application services
        return services;
    }
}
