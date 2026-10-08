using System.Net;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Logging.Abstractions;
using ReveilMusical.Domain;
using ReveilMusical.Infrastructure.Music;

namespace ReveilMusical.Infrastructure.Tests;

public class ItunesTrackProviderTests
{
    private static RateLimiter CreateRateLimiter(int tokenLimit = 20) => new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
    {
        TokenLimit = tokenLimit,
        TokensPerPeriod = tokenLimit,
        ReplenishmentPeriod = TimeSpan.FromMinutes(1),
        AutoReplenishment = false,
        QueueLimit = 0,
    });

    private static ItunesTrackProvider CreateProvider(
        Func<HttpRequestMessage, HttpResponseMessage> responder,
        RateLimiter? rateLimiter = null)
    {
        var handler = new FakeHttpMessageHandler(responder);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://itunes.apple.com/") };
        return new ItunesTrackProvider(httpClient, rateLimiter ?? CreateRateLimiter(), NullLogger<ItunesTrackProvider>.Instance);
    }

    private static HttpResponseMessage JsonFixture(string fileName) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(File.ReadAllText(Path.Combine("Fixtures", fileName)), Encoding.UTF8, "application/json"),
    };

    [Fact]
    public async Task FindAsync_FixtureWithResult_MapsToTrackWithoutProviderDetails()
    {
        var provider = CreateProvider(_ => JsonFixture("itunes-search.json"));

        var track = await provider.FindAsync(new TrackQuery("Here Comes the Sun"), CancellationToken.None);

        track.ShouldNotBeNull();
        track.Title.ShouldBe("Here Comes the Sun");
        track.Artist.ShouldBe("The Beatles");
    }

    [Fact]
    public async Task FindAsync_EmptyFixture_ReturnsNull()
    {
        var provider = CreateProvider(_ => JsonFixture("itunes-search-empty.json"));

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
    public async Task FindAsync_MalformedJson_Throws()
    {
        var provider = CreateProvider(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{not valid json", Encoding.UTF8, "application/json"),
        });

        await Should.ThrowAsync<Exception>(
            () => provider.FindAsync(new TrackQuery("anything"), CancellationToken.None));
    }

    [Fact]
    public async Task FindAsync_SearchTextWithSpaces_IsUrlEncoded()
    {
        HttpRequestMessage? capturedRequest = null;
        var provider = CreateProvider(request =>
        {
            capturedRequest = request;
            return JsonFixture("itunes-search-empty.json");
        });

        await provider.FindAsync(new TrackQuery("Here Comes the Sun"), CancellationToken.None);

        capturedRequest.ShouldNotBeNull();
        capturedRequest.RequestUri!.Query.ShouldContain("Here%20Comes%20the%20Sun");
    }

    [Fact]
    public async Task FindAsync_RateLimitExhausted_FailsFastWithoutCallingHttp()
    {
        var httpCalls = 0;
        var rateLimiter = CreateRateLimiter(tokenLimit: 20);
        var provider = CreateProvider(
            _ =>
            {
                httpCalls++;
                return JsonFixture("itunes-search-empty.json");
            },
            rateLimiter);

        for (var i = 0; i < 20; i++)
        {
            await provider.FindAsync(new TrackQuery("query"), CancellationToken.None);
        }

        await Should.ThrowAsync<RateLimitExceededException>(
            () => provider.FindAsync(new TrackQuery("one too many"), CancellationToken.None));
        httpCalls.ShouldBe(20);
    }
}
