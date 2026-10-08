using System.Net;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ReveilMusical.Application;
using ReveilMusical.Infrastructure.Music;

namespace ReveilMusical.Infrastructure.Tests;

public class MusicBrainzTrackProviderContractTests : TrackProviderContractTests
{
    protected override ITrackProvider CreateProviderReturningATrack()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(File.ReadAllText(Path.Combine("Fixtures", "musicbrainz-recording.json")), Encoding.UTF8, "application/json"),
        });
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://musicbrainz.org/ws/2/") };
        var options = Options.Create(new MusicOptions
        {
            MusicBrainz = new MusicBrainzOptions { UserAgent = "ReveilMusical/1.0 ( contact@example.test )" },
        });
        var rateLimiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            TokenLimit = 1,
            TokensPerPeriod = 1,
            ReplenishmentPeriod = TimeSpan.FromSeconds(1),
            AutoReplenishment = false,
            QueueLimit = 0,
        });
        return new MusicBrainzTrackProvider(httpClient, options, rateLimiter, NullLogger<MusicBrainzTrackProvider>.Instance);
    }
}
