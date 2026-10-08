using System.Net;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Logging.Abstractions;
using ReveilMusical.Application;
using ReveilMusical.Infrastructure.Music;

namespace ReveilMusical.Infrastructure.Tests;

public class ItunesTrackProviderContractTests : TrackProviderContractTests
{
    protected override ITrackProvider CreateProviderReturningATrack()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(File.ReadAllText(Path.Combine("Fixtures", "itunes-search.json")), Encoding.UTF8, "application/json"),
        });
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://itunes.apple.com/") };
        var rateLimiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            TokenLimit = 20,
            TokensPerPeriod = 20,
            ReplenishmentPeriod = TimeSpan.FromMinutes(1),
            AutoReplenishment = false,
            QueueLimit = 0,
        });
        return new ItunesTrackProvider(httpClient, rateLimiter, NullLogger<ItunesTrackProvider>.Instance);
    }
}
