using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly.Timeout;
using ReveilMusical.Infrastructure.Music;

namespace ReveilMusical.Infrastructure.Tests;

public class HttpClientBuilderExtensionsTests
{
    private sealed class SlowHttpMessageHandler(TimeSpan delay) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(delay, cancellationToken);
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task AddMusicProviderResilience_SlowResponse_TimesOutAroundThePerAttemptBudget()
    {
        var services = new ServiceCollection();
        services
            .AddHttpClient("slow-provider")
            .ConfigurePrimaryHttpMessageHandler(() => new SlowHttpMessageHandler(TimeSpan.FromSeconds(5)))
            .AddMusicProviderResilience(TimeSpan.FromMilliseconds(200));

        using var provider = services.BuildServiceProvider();
        var httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient("slow-provider");
        httpClient.BaseAddress = new Uri("https://example.test/");

        var stopwatch = Stopwatch.StartNew();
        await Should.ThrowAsync<TimeoutRejectedException>(() => httpClient.GetAsync("search"));
        stopwatch.Stop();

        stopwatch.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(3));
    }
}
