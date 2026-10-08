namespace ReveilMusical.Infrastructure.Music;

internal sealed class RateLimitExceededException(string providerName)
    : Exception($"Rate limit exceeded for provider '{providerName}'.");
