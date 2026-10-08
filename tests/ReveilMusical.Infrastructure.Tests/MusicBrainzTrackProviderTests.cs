using System.Net;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ReveilMusical.Domain;
using ReveilMusical.Infrastructure.Music;

namespace ReveilMusical.Infrastructure.Tests;

public class MusicBrainzTrackProviderTests
{
    private const string ContactUserAgent = "ReveilMusical/1.0 ( contact@example.test )";

    private static RateLimiter CreateRateLimiter(int tokenLimit = 1) => new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
    {
        TokenLimit = tokenLimit,
        TokensPerPeriod = tokenLimit,
        ReplenishmentPeriod = TimeSpan.FromSeconds(1),
        AutoReplenishment = false,
        QueueLimit = 0,
    });

    private static MusicBrainzTrackProvider CreateProvider(
        Func<HttpRequestMessage, HttpResponseMessage> responder,
        string userAgent = ContactUserAgent,
        RateLimiter? rateLimiter = null)
    {
        var handler = new FakeHttpMessageHandler(responder);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://musicbrainz.org/ws/2/") };
        var options = Options.Create(new MusicOptions { MusicBrainz = new MusicBrainzOptions { UserAgent = userAgent } });
        return new MusicBrainzTrackProvider(httpClient, options, rateLimiter ?? CreateRateLimiter(), NullLogger<MusicBrainzTrackProvider>.Instance);
    }

    private static HttpResponseMessage JsonFixture(string fileName) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(File.ReadAllText(Path.Combine("Fixtures", fileName)), Encoding.UTF8, "application/json"),
    };

    [Fact]
    public async Task FindAsync_FixtureWithResult_MapsToTrack()
    {
        var provider = CreateProvider(_ => JsonFixture("musicbrainz-recording.json"));

        var track = await provider.FindAsync(new TrackQuery("Here Comes the Sun"), CancellationToken.None);

        track.ShouldNotBeNull();
        track.Title.ShouldBe("Here Comes the Sun");
        track.Artist.ShouldBe("The Beatles");
    }

    [Fact]
    public async Task FindAsync_EmptyFixture_ReturnsNull()
    {
        var provider = CreateProvider(_ => JsonFixture("musicbrainz-recording-empty.json"));

        var track = await provider.FindAsync(new TrackQuery("Some obscure query"), CancellationToken.None);

        track.ShouldBeNull();
    }

    [Fact]
    public async Task FindAsync_ServerError_Throws()
    {
        var provider = CreateProvider(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        await Should.ThrowAsync<HttpRequestException>(
            () => provider.FindAsync(new TrackQuery("anything"), CancellationToken.None));
    }

    [Fact]
    public void Constructor_SetsUserAgentHeaderFromOptions()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonFixture("musicbrainz-recording-empty.json"));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://musicbrainz.org/ws/2/") };
        var options = Options.Create(new MusicOptions { MusicBrainz = new MusicBrainzOptions { UserAgent = ContactUserAgent } });

        _ = new MusicBrainzTrackProvider(httpClient, options, CreateRateLimiter(), NullLogger<MusicBrainzTrackProvider>.Instance);

        httpClient.DefaultRequestHeaders.UserAgent.ToString().ShouldBe(ContactUserAgent);
    }

    [Fact]
    public async Task FindAsync_EveryRequest_SendsConfiguredUserAgent()
    {
        HttpRequestMessage? capturedRequest = null;
        var provider = CreateProvider(request =>
        {
            capturedRequest = request;
            return JsonFixture("musicbrainz-recording-empty.json");
        });

        await provider.FindAsync(new TrackQuery("anything"), CancellationToken.None);

        capturedRequest.ShouldNotBeNull();
        capturedRequest.Headers.UserAgent.ToString().ShouldBe(ContactUserAgent);
    }

    [Fact]
    public async Task FindAsync_RateLimitExhausted_FailsFastWithoutCallingHttp()
    {
        var httpCalls = 0;
        var rateLimiter = CreateRateLimiter(tokenLimit: 1);
        var provider = CreateProvider(
            _ =>
            {
                httpCalls++;
                return JsonFixture("musicbrainz-recording-empty.json");
            },
            rateLimiter: rateLimiter);

        await provider.FindAsync(new TrackQuery("first"), CancellationToken.None);

        await Should.ThrowAsync<RateLimitExceededException>(
            () => provider.FindAsync(new TrackQuery("second, too soon"), CancellationToken.None));
        httpCalls.ShouldBe(1);
    }
}
