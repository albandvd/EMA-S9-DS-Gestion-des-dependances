using System.Collections.ObjectModel;

namespace ReveilMusical.Domain;

public sealed record UserPreferences
{
    public UserId UserId { get; }

    public IReadOnlyDictionary<WeatherType, TrackQuery> TracksByWeather { get; }

    public TrackQuery FallbackTrack { get; }

    public ChannelId PreferredChannel { get; }

    public IReadOnlyList<ContactPoint> ContactPoints { get; }

    public UserPreferences(
        UserId userId,
        IReadOnlyDictionary<WeatherType, TrackQuery> tracksByWeather,
        TrackQuery fallbackTrack,
        ChannelId preferredChannel,
        IReadOnlyList<ContactPoint> contactPoints)
    {
        UserId = userId;
        TracksByWeather = new ReadOnlyDictionary<WeatherType, TrackQuery>(
            tracksByWeather.ToDictionary(pair => pair.Key, pair => pair.Value));
        FallbackTrack = fallbackTrack;
        PreferredChannel = preferredChannel;
        ContactPoints = contactPoints;
    }

    public ContactPoint? FindContactPoint(ChannelId channel) =>
        ContactPoints.FirstOrDefault(contact => contact.Channel == channel);
}
