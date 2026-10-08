using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using ReveilMusical.Application;
using ReveilMusical.Domain;
using ReveilMusical.Infrastructure.Music;

namespace ReveilMusical.Infrastructure.Tests;

public class FallbackChainTrackResolverTests
{
    private static readonly Track ItunesTrack = new("iTunes Track", "iTunes Artist");
    private static readonly Track MusicBrainzTrack = new("MusicBrainz Track", "MusicBrainz Artist");

    private sealed class NeverRespondingTrackProvider : ITrackProvider
    {
        public Task<Track?> FindAsync(TrackQuery query, CancellationToken cancellationToken) =>
            Task.Delay(Timeout.Infinite, cancellationToken).ContinueWith(_ => (Track?)null, TaskScheduler.Default);
    }

    private static FallbackChainTrackResolver CreateResolver(
        ITrackProvider itunesProvider,
        ITrackProvider musicBrainzProvider,
        IEnumerable<string> providers,
        TimeProvider? timeProvider = null,
        int globalTimeoutSeconds = 5) =>
        new(
            itunesProvider,
            musicBrainzProvider,
            new LocalFallbackTrackProvider(),
            Options.Create(new MusicOptions { Providers = providers.ToList(), GlobalTimeoutSeconds = globalTimeoutSeconds }),
            timeProvider ?? TimeProvider.System,
            NullLogger<FallbackChainTrackResolver>.Instance);

    [Fact]
    public async Task ResolveAsync_FirstProviderSucceeds_ReturnsItsTrackNotDegraded()
    {
        var itunes = Substitute.For<ITrackProvider>();
        itunes.FindAsync(Arg.Any<TrackQuery>(), Arg.Any<CancellationToken>()).Returns(ItunesTrack);
        var musicBrainz = Substitute.For<ITrackProvider>();
        var resolver = CreateResolver(itunes, musicBrainz, ["itunes", "musicbrainz"]);

        var resolution = await resolver.ResolveAsync(new TrackQuery("query"), CancellationToken.None);

        resolution.Track.ShouldBe(ItunesTrack);
        resolution.Degraded.ShouldBeFalse();
        await musicBrainz.DidNotReceive().FindAsync(Arg.Any<TrackQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResolveAsync_FirstProviderThrows_FallsBackToNextProviderNotDegraded()
    {
        var itunes = Substitute.For<ITrackProvider>();
        itunes.FindAsync(Arg.Any<TrackQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<Track?>(new HttpRequestException("boom")));
        var musicBrainz = Substitute.For<ITrackProvider>();
        musicBrainz.FindAsync(Arg.Any<TrackQuery>(), Arg.Any<CancellationToken>()).Returns(MusicBrainzTrack);
        var resolver = CreateResolver(itunes, musicBrainz, ["itunes", "musicbrainz"]);

        var resolution = await resolver.ResolveAsync(new TrackQuery("query"), CancellationToken.None);

        resolution.Track.ShouldBe(MusicBrainzTrack);
        resolution.Degraded.ShouldBeFalse();
    }

    [Fact]
    public async Task ResolveAsync_AllConfiguredProvidersFail_FallsBackToLocalDegraded()
    {
        var itunes = Substitute.For<ITrackProvider>();
        itunes.FindAsync(Arg.Any<TrackQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<Track?>(new HttpRequestException("boom")));
        var musicBrainz = Substitute.For<ITrackProvider>();
        musicBrainz.FindAsync(Arg.Any<TrackQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<Track?>(new HttpRequestException("boom")));
        var resolver = CreateResolver(itunes, musicBrainz, ["itunes", "musicbrainz"]);

        var resolution = await resolver.ResolveAsync(new TrackQuery("query"), CancellationToken.None);

        resolution.Track.ShouldNotBeNull();
        resolution.Degraded.ShouldBeTrue();
    }

    [Fact]
    public async Task ResolveAsync_NoProviderFindsTheTrack_FallsBackToLocalDegraded()
    {
        var itunes = Substitute.For<ITrackProvider>();
        itunes.FindAsync(Arg.Any<TrackQuery>(), Arg.Any<CancellationToken>()).Returns((Track?)null);
        var musicBrainz = Substitute.For<ITrackProvider>();
        musicBrainz.FindAsync(Arg.Any<TrackQuery>(), Arg.Any<CancellationToken>()).Returns((Track?)null);
        var resolver = CreateResolver(itunes, musicBrainz, ["itunes", "musicbrainz"]);

        var resolution = await resolver.ResolveAsync(new TrackQuery("query"), CancellationToken.None);

        resolution.Track.ShouldNotBeNull();
        resolution.Degraded.ShouldBeTrue();
    }

    [Fact]
    public async Task ResolveAsync_EmptyProvidersList_UsesLocalDirectlyDegraded()
    {
        var itunes = Substitute.For<ITrackProvider>();
        var musicBrainz = Substitute.For<ITrackProvider>();
        var resolver = CreateResolver(itunes, musicBrainz, []);

        var resolution = await resolver.ResolveAsync(new TrackQuery("query"), CancellationToken.None);

        resolution.Degraded.ShouldBeTrue();
        await itunes.DidNotReceive().FindAsync(Arg.Any<TrackQuery>(), Arg.Any<CancellationToken>());
        await musicBrainz.DidNotReceive().FindAsync(Arg.Any<TrackQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResolveAsync_ProvidersOrderReversed_TriesMusicBrainzFirst()
    {
        var itunes = Substitute.For<ITrackProvider>();
        var musicBrainz = Substitute.For<ITrackProvider>();
        musicBrainz.FindAsync(Arg.Any<TrackQuery>(), Arg.Any<CancellationToken>()).Returns(MusicBrainzTrack);
        var resolver = CreateResolver(itunes, musicBrainz, ["musicbrainz", "itunes"]);

        var resolution = await resolver.ResolveAsync(new TrackQuery("query"), CancellationToken.None);

        resolution.Track.ShouldBe(MusicBrainzTrack);
        await itunes.DidNotReceive().FindAsync(Arg.Any<TrackQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResolveAsync_GlobalBudgetExceeded_StillCompletesWithLocalFallback()
    {
        var fakeTime = new FakeTimeProvider();
        var itunes = new NeverRespondingTrackProvider();
        var musicBrainz = Substitute.For<ITrackProvider>();
        var resolver = CreateResolver(itunes, musicBrainz, ["itunes"], fakeTime, globalTimeoutSeconds: 1);

        var resolveTask = resolver.ResolveAsync(new TrackQuery("query"), CancellationToken.None);
        fakeTime.Advance(TimeSpan.FromSeconds(2));
        var resolution = await resolveTask;

        resolution.Track.ShouldNotBeNull();
        resolution.Degraded.ShouldBeTrue();
    }
}
