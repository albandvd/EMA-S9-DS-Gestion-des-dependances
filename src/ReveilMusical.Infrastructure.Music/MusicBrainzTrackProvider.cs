using System.Net.Http.Json;
using System.Threading.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReveilMusical.Application;
using ReveilMusical.Domain;

namespace ReveilMusical.Infrastructure.Music;

internal sealed class MusicBrainzTrackProvider : ITrackProvider
{
    public const string RateLimiterKey = "musicbrainz-rate-limiter";

    private readonly HttpClient _httpClient;
    private readonly RateLimiter _rateLimiter;
    private readonly ILogger<MusicBrainzTrackProvider> _logger;

    public MusicBrainzTrackProvider(
        HttpClient httpClient,
        IOptions<MusicOptions> options,
        [FromKeyedServices(RateLimiterKey)] RateLimiter rateLimiter,
        ILogger<MusicBrainzTrackProvider> logger)
    {
        _httpClient = httpClient;
        _rateLimiter = rateLimiter;
        _logger = logger;

        if (_httpClient.DefaultRequestHeaders.UserAgent.Count == 0)
        {
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(options.Value.MusicBrainz.UserAgent);
        }
    }

    public async Task<Track?> FindAsync(TrackQuery query, CancellationToken cancellationToken)
    {
        using var lease = _rateLimiter.AttemptAcquire();
        if (!lease.IsAcquired)
        {
            _logger.LogWarning("MusicBrainz rate limit reached for query '{Query}'.", query);
            throw new RateLimitExceededException("musicbrainz");
        }

        var relativeUrl = $"recording?query={Uri.EscapeDataString(query.SearchText)}&fmt=json";
        var response = await _httpClient.GetFromJsonAsync<MusicBrainzSearchResponseDto>(relativeUrl, cancellationToken);

        var firstMatch = response?.Recordings.FirstOrDefault(r => r.Title is not null && r.ArtistCredit.Any(a => a.Name is not null));
        if (firstMatch is null)
        {
            return null;
        }

        var artistName = firstMatch.ArtistCredit.First(a => a.Name is not null).Name!;
        return new Track(firstMatch.Title!, artistName);
    }
}
