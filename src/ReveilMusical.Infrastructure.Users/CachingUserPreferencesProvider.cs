using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ReveilMusical.Application;
using ReveilMusical.Domain;

namespace ReveilMusical.Infrastructure.Users;

internal sealed class CachingUserPreferencesProvider : IUserPreferencesProvider
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);

    private readonly IUserPreferencesProvider _inner;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CachingUserPreferencesProvider> _logger;

    public CachingUserPreferencesProvider(
        IUserPreferencesProvider inner,
        IMemoryCache cache,
        ILogger<CachingUserPreferencesProvider> logger)
    {
        _inner = inner;
        _cache = cache;
        _logger = logger;
    }

    public async Task<PreferencesLookupResult> GetAsync(UserId userId, CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeyFor(userId);

        try
        {
            var result = await _inner.GetAsync(userId, cancellationToken);
            if (result.Preferences is not null)
            {
                _cache.Set(cacheKey, result.Preferences, CacheDuration);
            }

            return result;
        }
        catch (Exception exception)
        {
            if (_cache.TryGetValue(cacheKey, out UserPreferences? cachedPreferences) && cachedPreferences is not null)
            {
                var reason = "User preferences service unavailable; served the last known-good preferences from cache.";
                _logger.LogWarning(exception, "Preferences lookup failed for {UserId}, falling back to cache.", userId);
                return new PreferencesLookupResult(cachedPreferences, Degraded: true, DegradationReason: reason);
            }

            _logger.LogCritical(
                exception,
                "Preferences lookup failed for {UserId} and no cached value is available; the user cannot be notified.",
                userId);
            throw;
        }
    }

    private static string CacheKeyFor(UserId userId) => $"user-preferences:{userId.Value}";
}
