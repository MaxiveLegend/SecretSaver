using Microsoft.Extensions.DependencyInjection;

namespace SecretSaver.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<SecretService>();

        return services;
    }
}
