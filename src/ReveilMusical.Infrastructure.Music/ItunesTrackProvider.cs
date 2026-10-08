using System.Net.Http.Json;
using System.Threading.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReveilMusical.Application;
using ReveilMusical.Domain;

namespace ReveilMusical.Infrastructure.Music;

internal sealed class ItunesTrackProvider : ITrackProvider
{
    public const string RateLimiterKey = "itunes-rate-limiter";

    private readonly HttpClient _httpClient;
    private readonly IOptions<MusicOptions> _options;
    private readonly RateLimiter _rateLimiter;
    private readonly ILogger<ItunesTrackProvider> _logger;

    public ItunesTrackProvider(
        HttpClient httpClient,
        IOptions<MusicOptions> options,
        [FromKeyedServices(RateLimiterKey)] RateLimiter rateLimiter,
        ILogger<ItunesTrackProvider> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _rateLimiter = rateLimiter;
        _logger = logger;
    }

    public async Task<Track?> FindAsync(TrackQuery query, CancellationToken cancellationToken)
    {
        if (_options.Value.Itunes.SimulateFailure)
        {
            throw new InvalidOperationException("Simulated iTunes failure.");
        }

        using var lease = _rateLimiter.AttemptAcquire();
        if (!lease.IsAcquired)
        {
            _logger.LogWarning("iTunes rate limit reached for query '{Query}'.", query);
            throw new RateLimitExceededException("itunes");
        }

        var relativeUrl = $"search?term={Uri.EscapeDataString(query.SearchText)}&media=music&limit=5";
        var response = await _httpClient.GetFromJsonAsync<ItunesSearchResponseDto>(relativeUrl, cancellationToken);

        var firstResult = response?.Results.FirstOrDefault(r => r.TrackName is not null && r.ArtistName is not null);
        return firstResult is null ? null : new Track(firstResult.TrackName!, firstResult.ArtistName!);
    }
}
