namespace ReveilMusical.Domain;

public interface ITrackSelectionPolicy
{
    TrackQuery Select(UserPreferences preferences, DayOfWeek day, WeatherType weather);
}
