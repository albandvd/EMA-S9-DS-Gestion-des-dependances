using ReveilMusical.Domain;

namespace ReveilMusical.Application;

public sealed class WakeUpMessageComposer : IWakeUpMessageComposer
{
    private static readonly IReadOnlyDictionary<DayOfWeek, string> DayNames = new Dictionary<DayOfWeek, string>
    {
        [DayOfWeek.Monday] = "lundi",
        [DayOfWeek.Tuesday] = "mardi",
        [DayOfWeek.Wednesday] = "mercredi",
        [DayOfWeek.Thursday] = "jeudi",
        [DayOfWeek.Friday] = "vendredi",
        [DayOfWeek.Saturday] = "samedi",
        [DayOfWeek.Sunday] = "dimanche",
    };

    private static readonly IReadOnlyDictionary<WeatherType, string> WeatherPhrases = new Dictionary<WeatherType, string>
    {
        [WeatherType.Sunny] = "Il fait beau",
        [WeatherType.Rain] = "Il pleut",
        [WeatherType.Snow] = "Il neige",
        [WeatherType.Cloudy] = "Le temps est nuageux",
    };

    public WakeUpMessage Compose(UserId recipient, Track track, DayOfWeek day, WeatherType weather, bool musicDegraded)
    {
        var dayName = DayNames[day];
        var weatherPhrase = WeatherPhrases[weather];
        var text = $"Bon {dayName} ! {weatherPhrase} aujourd'hui, on se réveille avec {track.Title} — {track.Artist}.";

        if (musicDegraded)
        {
            text += " (morceau de secours, mode dégradé)";
        }

        return new WakeUpMessage(recipient, track, day, weather, text);
    }
}
