namespace ReveilMusical.Domain;

public sealed class WeatherBasedSelectionPolicy : ITrackSelectionPolicy
{
    public TrackQuery Select(UserPreferences preferences, DayOfWeek day, WeatherType weather) =>
        preferences.TracksByWeather.TryGetValue(weather, out var trackForWeather)
            ? trackForWeather
            : preferences.FallbackTrack;
}
