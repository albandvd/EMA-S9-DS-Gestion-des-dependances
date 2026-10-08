using Microsoft.Extensions.Options;
using ReveilMusical.Application;
using ReveilMusical.Domain;

namespace ReveilMusical.Infrastructure.Users;

internal sealed class InMemoryUserPreferencesProvider : IUserPreferencesProvider
{
    private readonly UsersOptions _options;

    public InMemoryUserPreferencesProvider(IOptions<UsersOptions> options)
    {
        _options = options.Value;
    }

    public Task<PreferencesLookupResult> GetAsync(UserId userId, CancellationToken cancellationToken)
    {
        if (_options.SimulateFailure)
        {
            throw new InvalidOperationException("User preferences service is simulating a failure.");
        }

        var profile = _options.Profiles.Find(p => p.UserId == userId.Value);
        var preferences = profile is null ? null : ToDomain(profile);

        return Task.FromResult(new PreferencesLookupResult(preferences, Degraded: false, DegradationReason: null));
    }

    private static UserPreferences ToDomain(UserProfileOptions profile)
    {
        var tracksByWeather = profile.TracksByWeather.ToDictionary(
            pair => Enum.Parse<WeatherType>(pair.Key, ignoreCase: true),
            pair => new TrackQuery(pair.Value));

        var contactPoints = profile.ContactPoints
            .Select(contact => new ContactPoint(new ChannelId(contact.Channel), contact.Address))
            .ToList();

        return new UserPreferences(
            new UserId(profile.UserId),
            tracksByWeather,
            new TrackQuery(profile.FallbackTrack),
            new ChannelId(profile.PreferredChannel),
            contactPoints);
    }
}
