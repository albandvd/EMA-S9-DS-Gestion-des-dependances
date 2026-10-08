using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ReveilMusical.Application;
using ReveilMusical.Domain;

namespace ReveilMusical.Api.Tests;

public class WakeUpEndpointTests
{
    [Fact]
    public async Task PostWakeUp_KnownUserAllHealthy_Returns200NonDegraded()
    {
        using var factory = WakeUpApiFactory.Create();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/wake-ups", new { userId = "u-1", dayOfWeek = "LUNDI", weather = "PLUIE" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<WakeUpResponse>();
        body.ShouldNotBeNull();
        body.Degraded.ShouldBeFalse();
        body.Track.Title.ShouldBe(WakeUpApiFactory.HealthyTrack.Title);
    }

    [Theory]
    [InlineData("u-1", "FUNDI", "PLUIE")]
    [InlineData("u-1", "LUNDI", "TORNADE")]
    [InlineData("", "LUNDI", "PLUIE")]
    public async Task PostWakeUp_InvalidInput_Returns400ProblemDetails(string userId, string day, string weather)
    {
        using var factory = WakeUpApiFactory.Create();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/wake-ups", new { userId, dayOfWeek = day, weather });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldContain("errors");
    }

    [Fact]
    public async Task PostWakeUp_UnknownUser_Returns404ProblemDetails()
    {
        using var factory = WakeUpApiFactory.Create();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/wake-ups", new { userId = "does-not-exist", dayOfWeek = "LUNDI", weather = "PLUIE" });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PostWakeUp_EverythingDown_Returns200DegradedRatherThanSilence()
    {
        using var factory = WakeUpApiFactory.Create(musicDegraded: true, notificationsDegraded: true);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/wake-ups", new { userId = "u-1", dayOfWeek = "LUNDI", weather = "PLUIE" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<WakeUpResponse>();
        body.ShouldNotBeNull();
        body.Degraded.ShouldBeTrue();
        body.Channel.ShouldBe("undelivered");
    }

    [Fact]
    public async Task PostWakeUp_PreferencesServiceDownNoCache_Returns503()
    {
        using var factory = WakeUpApiFactory.Create(configOverrides:
        [
            new("Users:SimulateFailure", "true"),
        ]);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/wake-ups", new { userId = "u-1", dayOfWeek = "LUNDI", weather = "PLUIE" });

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task PostWakeUp_ResponseBody_NeverLeaksProviderSpecificFields()
    {
        using var factory = WakeUpApiFactory.Create();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/wake-ups", new { userId = "u-1", dayOfWeek = "LUNDI", weather = "PLUIE" });
        var json = await response.Content.ReadAsStringAsync();

        using var document = JsonDocument.Parse(json);
        var trackProperties = document.RootElement.GetProperty("track").EnumerateObject().Select(p => p.Name).ToList();

        trackProperties.ShouldBe(["title", "artist"], ignoreOrder: true);
        json.ShouldNotContain("trackViewUrl", Case.Insensitive);
        json.ShouldNotContain("artistCredit", Case.Insensitive);
    }

    [Fact]
    public async Task PostWakeUp_SwitchingMusicProvidersByConfig_ChangesWhichTrackIsUsed_NoRecompilation()
    {
        var itunesTrack = new Track("iTunes Pick", "iTunes Artist");
        var musicBrainzTrack = new Track("MusicBrainz Pick", "MusicBrainz Artist");

        using var itunesFirstFactory = BuildFactoryWithDistinctProviders(itunesTrack, musicBrainzTrack, firstProviderKey: "itunes");
        using var itunesFirstClient = itunesFirstFactory.CreateClient();
        var itunesFirstResponse = await itunesFirstClient.PostAsJsonAsync("/api/wake-ups", new { userId = "u-1", dayOfWeek = "LUNDI", weather = "PLUIE" });
        var itunesFirstBody = await itunesFirstResponse.Content.ReadFromJsonAsync<WakeUpResponse>();

        using var musicBrainzFirstFactory = BuildFactoryWithDistinctProviders(itunesTrack, musicBrainzTrack, firstProviderKey: "musicbrainz");
        using var musicBrainzFirstClient = musicBrainzFirstFactory.CreateClient();
        var musicBrainzFirstResponse = await musicBrainzFirstClient.PostAsJsonAsync("/api/wake-ups", new { userId = "u-1", dayOfWeek = "LUNDI", weather = "PLUIE" });
        var musicBrainzFirstBody = await musicBrainzFirstResponse.Content.ReadFromJsonAsync<WakeUpResponse>();

        itunesFirstBody.ShouldNotBeNull();
        musicBrainzFirstBody.ShouldNotBeNull();
        itunesFirstBody.Track.Title.ShouldBe(itunesTrack.Title);
        musicBrainzFirstBody.Track.Title.ShouldBe(musicBrainzTrack.Title);
        itunesFirstBody.Track.Title.ShouldNotBe(musicBrainzFirstBody.Track.Title);
    }

    private static WebApplicationFactory<Program> BuildFactoryWithDistinctProviders(Track itunesTrack, Track musicBrainzTrack, string firstProviderKey)
    {
        var factory = new WebApplicationFactory<Program>();
        return factory.WithWebHostBuilder(builder =>
        {
            var secondProviderKey = firstProviderKey == "itunes" ? "musicbrainz" : "itunes";
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Music:Providers:0"] = firstProviderKey,
                ["Music:Providers:1"] = secondProviderKey,
            }));
            builder.ConfigureTestServices(services =>
            {
                services.AddKeyedSingleton<ITrackProvider>("itunes", new TestTrackProvider(itunesTrack));
                services.AddKeyedSingleton<ITrackProvider>("musicbrainz", new TestTrackProvider(musicBrainzTrack));
            });
        });
    }
}
