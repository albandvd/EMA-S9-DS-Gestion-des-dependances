using ReveilMusical.Domain;

namespace ReveilMusical.Api;

internal static class WakeUpRequestMapping
{
    private static readonly IReadOnlyDictionary<string, DayOfWeek> DayLabels = new Dictionary<string, DayOfWeek>(StringComparer.OrdinalIgnoreCase)
    {
        ["LUNDI"] = DayOfWeek.Monday,
        ["MONDAY"] = DayOfWeek.Monday,
        ["MARDI"] = DayOfWeek.Tuesday,
        ["TUESDAY"] = DayOfWeek.Tuesday,
        ["MERCREDI"] = DayOfWeek.Wednesday,
        ["WEDNESDAY"] = DayOfWeek.Wednesday,
        ["JEUDI"] = DayOfWeek.Thursday,
        ["THURSDAY"] = DayOfWeek.Thursday,
        ["VENDREDI"] = DayOfWeek.Friday,
        ["FRIDAY"] = DayOfWeek.Friday,
        ["SAMEDI"] = DayOfWeek.Saturday,
        ["SATURDAY"] = DayOfWeek.Saturday,
        ["DIMANCHE"] = DayOfWeek.Sunday,
        ["SUNDAY"] = DayOfWeek.Sunday,
    };

    private static readonly IReadOnlyDictionary<string, WeatherType> WeatherLabels = new Dictionary<string, WeatherType>(StringComparer.OrdinalIgnoreCase)
    {
        ["SOLEIL"] = WeatherType.Sunny,
        ["PLUIE"] = WeatherType.Rain,
        ["NEIGE"] = WeatherType.Snow,
        ["NUAGEUX"] = WeatherType.Cloudy,
    };

    public static bool TryParseDay(string value, out DayOfWeek day) => DayLabels.TryGetValue(value, out day);

    public static bool TryParseWeather(string value, out WeatherType weather) => WeatherLabels.TryGetValue(value, out weather);
}
