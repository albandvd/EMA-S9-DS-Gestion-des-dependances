using ReveilMusical.Domain;

namespace ReveilMusical.Application;

public interface IWakeUpMessageComposer
{
    WakeUpMessage Compose(UserId recipient, Track track, DayOfWeek day, WeatherType weather, bool musicDegraded);
}
