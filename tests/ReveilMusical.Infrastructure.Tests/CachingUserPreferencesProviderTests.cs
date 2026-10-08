using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using ReveilMusical.Application;
using ReveilMusical.Domain;
using ReveilMusical.Infrastructure.Users;

namespace ReveilMusical.Infrastructure.Tests;

public class CachingUserPreferencesProviderTests
{
    private static readonly UserId AnyUserId = new("u-42");

    private static UserPreferences CreatePreferences() => new(
        AnyUserId,
        new Dictionary<WeatherType, TrackQuery>(),
        new TrackQuery("fallback"),
        new ChannelId("email"),
        [new ContactPoint(new ChannelId("email"), "user@example.com")]);

    private static CachingUserPreferencesProvider CreateProvider(IUserPreferencesProvider inner, IMemoryCache? cache = null) =>
        new(inner, cache ?? new MemoryCache(new MemoryCacheOptions()), NullLogger<CachingUserPreferencesProvider>.Instance);

    [Fact]
    public async Task GetAsync_InnerSucceeds_ReturnsResultUnchanged()
    {
        var preferences = CreatePreferences();
        var inner = Substitute.For<IUserPreferencesProvider>();
        inner.GetAsync(AnyUserId, Arg.Any<CancellationToken>())
            .Returns(new PreferencesLookupResult(preferences, Degraded: false, DegradationReason: null));

        var result = await CreateProvider(inner).GetAsync(AnyUserId, CancellationToken.None);

        result.Preferences.ShouldBe(preferences);
        result.Degraded.ShouldBeFalse();
    }

    [Fact]
    public async Task GetAsync_InnerFailsAfterPriorSuccess_ReturnsCachedPreferencesDegraded()
    {
        var preferences = CreatePreferences();
        var inner = Substitute.For<IUserPreferencesProvider>();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var provider = CreateProvider(inner, cache);

        inner.GetAsync(AnyUserId, Arg.Any<CancellationToken>())
            .Returns(new PreferencesLookupResult(preferences, Degraded: false, DegradationReason: null));
        await provider.GetAsync(AnyUserId, CancellationToken.None);

        inner.GetAsync(AnyUserId, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<PreferencesLookupResult>(new InvalidOperationException("service down")));
        var result = await provider.GetAsync(AnyUserId, CancellationToken.None);

        result.Preferences.ShouldBe(preferences);
        result.Degraded.ShouldBeTrue();
    }

    [Fact]
    public async Task GetAsync_InnerFailsWithoutCache_Throws()
    {
        var inner = Substitute.For<IUserPreferencesProvider>();
        inner.GetAsync(AnyUserId, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<PreferencesLookupResult>(new InvalidOperationException("service down")));

        await Should.ThrowAsync<InvalidOperationException>(
            () => CreateProvider(inner).GetAsync(AnyUserId, CancellationToken.None));
    }
}
