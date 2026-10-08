using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReveilMusical.Application;
using ReveilMusical.Domain;

namespace ReveilMusical.Infrastructure.Music;

internal sealed class FallbackChainTrackResolver : ITrackResolver
{
    private readonly IReadOnlyDictionary<string, ITrackProvider> _providersByKey;
    private readonly LocalFallbackTrackProvider _localProvider;
    private readonly MusicOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<FallbackChainTrackResolver> _logger;

    public FallbackChainTrackResolver(
        [FromKeyedServices("itunes")] ITrackProvider itunesProvider,
        [FromKeyedServices("musicbrainz")] ITrackProvider musicBrainzProvider,
        LocalFallbackTrackProvider localProvider,
        IOptions<MusicOptions> options,
        TimeProvider timeProvider,
        ILogger<FallbackChainTrackResolver> logger)
    {
        _providersByKey = new Dictionary<string, ITrackProvider>(StringComparer.OrdinalIgnoreCase)
        {
            ["itunes"] = itunesProvider,
            ["musicbrainz"] = musicBrainzProvider,
        };
        _localProvider = localProvider;
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<TrackResolution> ResolveAsync(TrackQuery query, CancellationToken cancellationToken)
    {
        using var budgetCts = new CancellationTokenSource(TimeSpan.FromSeconds(_options.GlobalTimeoutSeconds), _timeProvider);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, budgetCts.Token);

        foreach (var key in _options.Providers)
        {
            if (!_providersByKey.TryGetValue(key, out var provider))
            {
                _logger.LogWarning("Music provider '{Key}' is not registered; skipping.", key);
                continue;
            }

            try
            {
                var track = await provider.FindAsync(query, linkedCts.Token);
                if (track is not null)
                {
                    return new TrackResolution(track, Degraded: false);
                }

                _logger.LogInformation("Music provider '{Key}' found no match for query '{Query}'.", key, query);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Music provider '{Key}' failed for query '{Query}'.", key, query);
            }
        }

        var fallbackTrack = await _localProvider.FindAsync(query, CancellationToken.None);
        return new TrackResolution(fallbackTrack!, Degraded: true);
    }
}
