using ReveilMusical.Application;
using ReveilMusical.Domain;

namespace ReveilMusical.Application.Tests;

public class WakeUpMessageComposerTests
{
    private readonly WakeUpMessageComposer _composer = new();
    private static readonly UserId AnyUserId = new("u-42");
    private static readonly Track AnyTrack = new("Riders on the Storm", "The Doors");

    public static IEnumerable<object[]> AllDaysAndWeathers()
    {
        foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
        {
            foreach (WeatherType weather in Enum.GetValues<WeatherType>())
            {
                yield return [day, weather];
            }
        }
    }

    [Theory]
    [MemberData(nameof(AllDaysAndWeathers))]
    public void Compose_AnyDayAndWeather_MentionsTrackAndDoesNotThrow(DayOfWeek day, WeatherType weather)
    {
        var message = _composer.Compose(AnyUserId, AnyTrack, day, weather, musicDegraded: false);

        message.Text.ShouldContain(AnyTrack.Title);
        message.Text.ShouldContain(AnyTrack.Artist);
        message.Day.ShouldBe(day);
        message.Weather.ShouldBe(weather);
    }

    [Fact]
    public void Compose_MusicDegraded_MentionsDegradedMode()
    {
        var message = _composer.Compose(AnyUserId, AnyTrack, DayOfWeek.Monday, WeatherType.Rain, musicDegraded: true);

        message.Text.ShouldContain("dégradé");
    }

    [Fact]
    public void Compose_MusicNotDegraded_DoesNotMentionDegradedMode()
    {
        var message = _composer.Compose(AnyUserId, AnyTrack, DayOfWeek.Monday, WeatherType.Rain, musicDegraded: false);

        message.Text.ShouldNotContain("dégradé");
    }
}
