using Microsoft.Extensions.Caching.Memory;
using ReveilMusical.Application;
using ReveilMusical.Domain;
using ReveilMusical.Infrastructure.Music;

namespace ReveilMusical.Infrastructure.Tests;

public class CachingTrackProviderTests
{
    private static CachingTrackProvider CreateProvider(ITrackProvider inner, IMemoryCache? cache = null) =>
        new(inner, cache ?? new MemoryCache(new MemoryCacheOptions()), TimeSpan.FromHours(24), "test-provider");

    [Fact]
    public async Task FindAsync_SecondIdenticalQuery_DoesNotCallInnerAgain()
    {
        var calls = 0;
        var inner = Substitute.For<ITrackProvider>();
        inner.FindAsync(Arg.Any<TrackQuery>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                calls++;
                return Task.FromResult<Track?>(new Track("Here Comes the Sun", "The Beatles"));
            });
        var provider = CreateProvider(inner);

        await provider.FindAsync(new TrackQuery("Here Comes the Sun"), CancellationToken.None);
        await provider.FindAsync(new TrackQuery("Here Comes the Sun"), CancellationToken.None);

        calls.ShouldBe(1);
    }

    [Fact]
    public async Task FindAsync_NotFoundResult_IsAlsoCached()
    {
        var calls = 0;
        var inner = Substitute.For<ITrackProvider>();
        inner.FindAsync(Arg.Any<TrackQuery>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                calls++;
                return Task.FromResult<Track?>(null);
            });
        var provider = CreateProvider(inner);

        var first = await provider.FindAsync(new TrackQuery("unknown"), CancellationToken.None);
        var second = await provider.FindAsync(new TrackQuery("unknown"), CancellationToken.None);

        first.ShouldBeNull();
        second.ShouldBeNull();
        calls.ShouldBe(1);
    }

    [Fact]
    public async Task FindAsync_DifferentQueries_AreNotConflated()
    {
        var inner = Substitute.For<ITrackProvider>();
        inner.FindAsync(new TrackQuery("a"), Arg.Any<CancellationToken>())
            .Returns(new Track("Track A", "Artist A"));
        inner.FindAsync(new TrackQuery("b"), Arg.Any<CancellationToken>())
            .Returns(new Track("Track B", "Artist B"));
        var provider = CreateProvider(inner);

        var resultA = await provider.FindAsync(new TrackQuery("a"), CancellationToken.None);
        var resultB = await provider.FindAsync(new TrackQuery("b"), CancellationToken.None);

        resultA!.Title.ShouldBe("Track A");
        resultB!.Title.ShouldBe("Track B");
    }
}
