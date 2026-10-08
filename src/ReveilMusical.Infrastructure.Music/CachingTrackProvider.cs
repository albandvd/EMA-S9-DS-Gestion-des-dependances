using Microsoft.Extensions.Caching.Memory;
using ReveilMusical.Application;
using ReveilMusical.Domain;

namespace ReveilMusical.Infrastructure.Music;

internal sealed class CachingTrackProvider : ITrackProvider
{
    private readonly ITrackProvider _inner;
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _ttl;
    private readonly string _cacheKeyPrefix;

    public CachingTrackProvider(ITrackProvider inner, IMemoryCache cache, TimeSpan ttl, string cacheKeyPrefix)
    {
        _inner = inner;
        _cache = cache;
        _ttl = ttl;
        _cacheKeyPrefix = cacheKeyPrefix;
    }

    public async Task<Track?> FindAsync(TrackQuery query, CancellationToken cancellationToken)
    {
        var cacheKey = $"track-provider:{_cacheKeyPrefix}:{query.SearchText.Trim().ToLowerInvariant()}";

        if (_cache.TryGetValue(cacheKey, out Track? cached))
        {
            return cached;
        }

        var result = await _inner.FindAsync(query, cancellationToken);
        _cache.Set(cacheKey, result, _ttl);
        return result;
    }
}
