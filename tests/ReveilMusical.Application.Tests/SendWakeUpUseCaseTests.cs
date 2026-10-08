using Microsoft.Extensions.Logging.Abstractions;
using ReveilMusical.Application;
using ReveilMusical.Domain;

namespace ReveilMusical.Application.Tests;

public class SendWakeUpUseCaseTests
{
    private static readonly UserId AnyUserId = new("u-42");
    private static readonly Track ResolvedTrack = new("Riders on the Storm", "The Doors");
    private static readonly WakeUpMessage ComposedMessage = new(AnyUserId, ResolvedTrack, DayOfWeek.Monday, WeatherType.Rain, "Bon lundi !");

    private readonly IUserPreferencesProvider _preferencesProvider = Substitute.For<IUserPreferencesProvider>();
    private readonly ITrackSelectionPolicy _selectionPolicy = Substitute.For<ITrackSelectionPolicy>();
    private readonly ITrackResolver _trackResolver = Substitute.For<ITrackResolver>();
    private readonly INotificationDispatcher _notificationDispatcher = Substitute.For<INotificationDispatcher>();
    private readonly IWakeUpMessageComposer _messageComposer = Substitute.For<IWakeUpMessageComposer>();

    private SendWakeUpUseCase CreateUseCase() => new(
        _preferencesProvider,
        _selectionPolicy,
        _trackResolver,
        _notificationDispatcher,
        _messageComposer,
        NullLogger<SendWakeUpUseCase>.Instance);

    private static UserPreferences CreatePreferences() => new(
        AnyUserId,
        new Dictionary<WeatherType, TrackQuery>(),
        new TrackQuery("Here Comes the Sun"),
        new ChannelId("email"),
        [new ContactPoint(new ChannelId("email"), "user@example.com")]);

    private void ArrangeHealthyPipeline(UserPreferences preferences, bool trackResolverDegraded = false, bool dispatchDegraded = false)
    {
        _preferencesProvider.GetAsync(AnyUserId, Arg.Any<CancellationToken>())
            .Returns(new PreferencesLookupResult(preferences, Degraded: false, DegradationReason: null));
        _selectionPolicy.Select(preferences, Arg.Any<DayOfWeek>(), Arg.Any<WeatherType>())
            .Returns(preferences.FallbackTrack);
        _trackResolver.ResolveAsync(Arg.Any<TrackQuery>(), Arg.Any<CancellationToken>())
            .Returns(new TrackResolution(ResolvedTrack, trackResolverDegraded));
        _messageComposer.Compose(AnyUserId, ResolvedTrack, Arg.Any<DayOfWeek>(), Arg.Any<WeatherType>(), Arg.Any<bool>())
            .Returns(ComposedMessage);
        _notificationDispatcher.DispatchAsync(ComposedMessage, preferences, Arg.Any<CancellationToken>())
            .Returns(new ChannelDispatchResult(
                preferences.PreferredChannel,
                dispatchDegraded,
                dispatchDegraded ? ["Preferred channel unavailable, used fallback order."] : []));
    }

    [Fact]
    public async Task ExecuteAsync_UnknownUser_ReturnsNull()
    {
        _preferencesProvider.GetAsync(AnyUserId, Arg.Any<CancellationToken>())
            .Returns(new PreferencesLookupResult(Preferences: null, Degraded: false, DegradationReason: null));

        var outcome = await CreateUseCase().ExecuteAsync(AnyUserId, DayOfWeek.Monday, WeatherType.Rain, CancellationToken.None);

        outcome.ShouldBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_AllHealthy_ReturnsNonDegradedOutcome()
    {
        var preferences = CreatePreferences();
        ArrangeHealthyPipeline(preferences);

        var outcome = await CreateUseCase().ExecuteAsync(AnyUserId, DayOfWeek.Monday, WeatherType.Rain, CancellationToken.None);

        outcome.ShouldNotBeNull();
        outcome.Degraded.ShouldBeFalse();
        outcome.MusicDegraded.ShouldBeFalse();
        outcome.ChannelDegraded.ShouldBeFalse();
        outcome.Track.ShouldBe(ResolvedTrack);
        outcome.ChannelUsed.ShouldBe(preferences.PreferredChannel);
    }

    [Fact]
    public async Task ExecuteAsync_PreferencesServedFromCache_MarksOutcomeDegradedWithReason()
    {
        var preferences = CreatePreferences();
        ArrangeHealthyPipeline(preferences);
        _preferencesProvider.GetAsync(AnyUserId, Arg.Any<CancellationToken>())
            .Returns(new PreferencesLookupResult(preferences, Degraded: true, DegradationReason: "Preferences service unavailable, served from cache."));

        var outcome = await CreateUseCase().ExecuteAsync(AnyUserId, DayOfWeek.Monday, WeatherType.Rain, CancellationToken.None);

        outcome.ShouldNotBeNull();
        outcome.Degraded.ShouldBeTrue();
        outcome.MusicDegraded.ShouldBeFalse();
        outcome.ChannelDegraded.ShouldBeFalse();
        outcome.DegradationReasons.ShouldContain("Preferences service unavailable, served from cache.");
    }

    [Fact]
    public async Task ExecuteAsync_TrackResolverReturnsDegradedResolution_MarksMusicDegraded()
    {
        var preferences = CreatePreferences();
        ArrangeHealthyPipeline(preferences, trackResolverDegraded: true);

        var outcome = await CreateUseCase().ExecuteAsync(AnyUserId, DayOfWeek.Monday, WeatherType.Rain, CancellationToken.None);

        outcome.ShouldNotBeNull();
        outcome.MusicDegraded.ShouldBeTrue();
        outcome.Degraded.ShouldBeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_TrackResolverThrows_UsesRawFallbackTrackAndMarksMusicDegraded()
    {
        var preferences = CreatePreferences();
        ArrangeHealthyPipeline(preferences);
        _trackResolver.ResolveAsync(Arg.Any<TrackQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<TrackResolution>(new InvalidOperationException("boom")));
        _messageComposer
            .Compose(AnyUserId, Arg.Any<Track>(), Arg.Any<DayOfWeek>(), Arg.Any<WeatherType>(), Arg.Any<bool>())
            .Returns(ComposedMessage);
        _notificationDispatcher.DispatchAsync(ComposedMessage, preferences, Arg.Any<CancellationToken>())
            .Returns(new ChannelDispatchResult(preferences.PreferredChannel, Degraded: false, Reasons: []));

        var outcome = await CreateUseCase().ExecuteAsync(AnyUserId, DayOfWeek.Monday, WeatherType.Rain, CancellationToken.None);

        outcome.ShouldNotBeNull();
        outcome.MusicDegraded.ShouldBeTrue();
        outcome.Track.Title.ShouldBe(preferences.FallbackTrack.SearchText);
    }

    [Fact]
    public async Task ExecuteAsync_DispatchDegraded_MarksChannelDegradedAndAggregatesReasons()
    {
        var preferences = CreatePreferences();
        ArrangeHealthyPipeline(preferences, dispatchDegraded: true);

        var outcome = await CreateUseCase().ExecuteAsync(AnyUserId, DayOfWeek.Monday, WeatherType.Rain, CancellationToken.None);

        outcome.ShouldNotBeNull();
        outcome.ChannelDegraded.ShouldBeTrue();
        outcome.Degraded.ShouldBeTrue();
        outcome.DegradationReasons.ShouldContain("Preferred channel unavailable, used fallback order.");
    }

    [Fact]
    public async Task ExecuteAsync_UsesInjectedSelectionPolicy_ProvesPolicyIsPluggable()
    {
        var preferences = CreatePreferences();
        ArrangeHealthyPipeline(preferences);
        var alternativeQuery = new TrackQuery("Weekend playlist override");
        _selectionPolicy.Select(preferences, DayOfWeek.Saturday, Arg.Any<WeatherType>()).Returns(alternativeQuery);
        _trackResolver.ResolveAsync(alternativeQuery, Arg.Any<CancellationToken>())
            .Returns(new TrackResolution(ResolvedTrack, Degraded: false));

        await CreateUseCase().ExecuteAsync(AnyUserId, DayOfWeek.Saturday, WeatherType.Rain, CancellationToken.None);

        _selectionPolicy.Received(1).Select(preferences, DayOfWeek.Saturday, Arg.Any<WeatherType>());
        await _trackResolver.Received(1).ResolveAsync(alternativeQuery, Arg.Any<CancellationToken>());
    }
}
