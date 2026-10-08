using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace ReveilMusical.Infrastructure.Music;

internal static class HttpClientBuilderExtensions
{
    public static IHttpClientBuilder AddMusicProviderResilience(this IHttpClientBuilder builder, TimeSpan perAttemptTimeout)
    {
        builder.AddResilienceHandler("music-provider", pipelineBuilder =>
        {
            pipelineBuilder.AddTimeout(perAttemptTimeout);
            pipelineBuilder.AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = 1,
                UseJitter = true,
                Delay = TimeSpan.FromMilliseconds(200),
            });
            pipelineBuilder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions());
        });

        return builder;
    }
}
