using ReveilMusical.Domain;

namespace ReveilMusical.Domain.Tests;

public class WeatherBasedSelectionPolicyTests
{
    private readonly WeatherBasedSelectionPolicy _policy = new();

    [Theory]
    [InlineData(WeatherType.Sunny)]
    [InlineData(WeatherType.Rain)]
    [InlineData(WeatherType.Snow)]
    [InlineData(WeatherType.Cloudy)]
    public void Select_WeatherCovered_ReturnsTrackForWeather(WeatherType weather)
    {
        var expected = new TrackQuery($"Track for {weather}");
        var preferences = CreatePreferences(new Dictionary<WeatherType, TrackQuery> { [weather] = expected });

        var selected = _policy.Select(preferences, DayOfWeek.Monday, weather);

        selected.ShouldBe(expected);
    }

    [Fact]
    public void Select_WeatherNotCovered_ReturnsFallbackTrack()
    {
        var preferences = CreatePreferences(new Dictionary<WeatherType, TrackQuery>
        {
            [WeatherType.Sunny] = new TrackQuery("Here Comes the Sun"),
        });

        var selected = _policy.Select(preferences, DayOfWeek.Monday, WeatherType.Snow);

        selected.ShouldBe(preferences.FallbackTrack);
    }

    private static UserPreferences CreatePreferences(IReadOnlyDictionary<WeatherType, TrackQuery> tracksByWeather) =>
        new(
            new UserId("u-42"),
            tracksByWeather,
            new TrackQuery("Fallback track"),
            new ChannelId("email"),
            [new ContactPoint(new ChannelId("email"), "user@example.com")]);
}
