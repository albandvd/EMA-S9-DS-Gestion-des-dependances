using Microsoft.Extensions.Options;
using ReveilMusical.Domain;
using ReveilMusical.Infrastructure.Users;

namespace ReveilMusical.Infrastructure.Tests;

public class InMemoryUserPreferencesProviderTests
{
    private static InMemoryUserPreferencesProvider CreateProvider(UsersOptions options) =>
        new(Options.Create(options));

    private static UsersOptions CreateOptions(bool simulateFailure = false) => new()
    {
        SimulateFailure = simulateFailure,
        Profiles =
        [
            new UserProfileOptions
            {
                UserId = "u-42",
                TracksByWeather = new Dictionary<string, string> { ["Rain"] = "Riders on the Storm" },
                FallbackTrack = "Here Comes the Sun",
                PreferredChannel = "email",
                ContactPoints = [new ContactPointOptions { Channel = "email", Address = "user@example.com" }],
            },
        ],
    };

    [Fact]
    public async Task GetAsync_KnownUser_ReturnsMappedPreferences()
    {
        var provider = CreateProvider(CreateOptions());

        var result = await provider.GetAsync(new UserId("u-42"), CancellationToken.None);

        result.Preferences.ShouldNotBeNull();
        result.Degraded.ShouldBeFalse();
        result.Preferences.PreferredChannel.ShouldBe(new ChannelId("email"));
        result.Preferences.TracksByWeather[WeatherType.Rain].ShouldBe(new TrackQuery("Riders on the Storm"));
    }

    [Fact]
    public async Task GetAsync_UnknownUser_ReturnsNullPreferences()
    {
        var provider = CreateProvider(CreateOptions());

        var result = await provider.GetAsync(new UserId("unknown"), CancellationToken.None);

        result.Preferences.ShouldBeNull();
    }

    [Fact]
    public async Task GetAsync_SimulatedFailure_Throws()
    {
        var provider = CreateProvider(CreateOptions(simulateFailure: true));

        await Should.ThrowAsync<InvalidOperationException>(
            () => provider.GetAsync(new UserId("u-42"), CancellationToken.None));
    }
}
