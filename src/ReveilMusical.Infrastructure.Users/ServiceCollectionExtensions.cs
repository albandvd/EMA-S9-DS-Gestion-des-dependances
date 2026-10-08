using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReveilMusical.Application;

namespace ReveilMusical.Infrastructure.Users;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUserPreferences(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<UsersOptions>()
            .Bind(configuration.GetSection(UsersOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddMemoryCache();
        services.AddSingleton<InMemoryUserPreferencesProvider>();
        services.AddSingleton<IUserPreferencesProvider>(provider => new CachingUserPreferencesProvider(
            provider.GetRequiredService<InMemoryUserPreferencesProvider>(),
            provider.GetRequiredService<IMemoryCache>(),
            provider.GetRequiredService<ILogger<CachingUserPreferencesProvider>>()));

        return services;
    }
}
