using System.Threading.RateLimiting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using ReveilMusical.Application;

namespace ReveilMusical.Infrastructure.Music;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMusicProviders(this IServiceCollection services, IConfiguration configuration)
    {
        var musicOptions = configuration.GetSection(MusicOptions.SectionName).Get<MusicOptions>() ?? new MusicOptions();

        services.TryAddSingleton(TimeProvider.System);
        services.AddMemoryCache();

        services
            .AddOptions<MusicOptions>()
            .Bind(configuration.GetSection(MusicOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<MusicOptions>, MusicOptionsValidation>();

        services.AddKeyedSingleton<RateLimiter>(
            ItunesTrackProvider.RateLimiterKey,
            (_, _) => new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = musicOptions.Itunes.RequestsPerMinute,
                TokensPerPeriod = musicOptions.Itunes.RequestsPerMinute,
                ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                AutoReplenishment = true,
                QueueLimit = 0,
            }));

        services.AddKeyedSingleton<RateLimiter>(
            MusicBrainzTrackProvider.RateLimiterKey,
            (_, _) => new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 1,
                TokensPerPeriod = 1,
                ReplenishmentPeriod = TimeSpan.FromSeconds(1),
                AutoReplenishment = true,
                QueueLimit = 0,
            }));

        services
            .AddHttpClient<ItunesTrackProvider>(client => client.BaseAddress = new Uri(musicOptions.Itunes.BaseUrl))
            .AddMusicProviderResilience(TimeSpan.FromSeconds(musicOptions.Itunes.TimeoutSeconds));

        services
            .AddHttpClient<MusicBrainzTrackProvider>(client => client.BaseAddress = new Uri(musicOptions.MusicBrainz.BaseUrl))
            .AddMusicProviderResilience(TimeSpan.FromSeconds(musicOptions.MusicBrainz.TimeoutSeconds));

        services.AddKeyedSingleton<ITrackProvider>("itunes", (provider, _) => new CachingTrackProvider(
            provider.GetRequiredService<ItunesTrackProvider>(),
            provider.GetRequiredService<IMemoryCache>(),
            TimeSpan.FromHours(musicOptions.CacheTtlHours),
            "itunes"));

        services.AddKeyedSingleton<ITrackProvider>("musicbrainz", (provider, _) => new CachingTrackProvider(
            provider.GetRequiredService<MusicBrainzTrackProvider>(),
            provider.GetRequiredService<IMemoryCache>(),
            TimeSpan.FromHours(musicOptions.CacheTtlHours),
            "musicbrainz"));

        services.AddSingleton<LocalFallbackTrackProvider>();
        services.AddSingleton<ITrackResolver, FallbackChainTrackResolver>();

        return services;
    }
}
