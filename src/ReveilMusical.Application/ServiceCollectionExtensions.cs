using Microsoft.Extensions.DependencyInjection;
using ReveilMusical.Domain;

namespace ReveilMusical.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddReveilMusicalApplication(this IServiceCollection services)
    {
        services.AddSingleton<ITrackSelectionPolicy, WeatherBasedSelectionPolicy>();
        services.AddSingleton<IWakeUpMessageComposer, WakeUpMessageComposer>();
        services.AddSingleton<SendWakeUpUseCase>();

        return services;
    }
}
