using ReveilMusical.Domain;

namespace ReveilMusical.Domain.Tests;

public class UserPreferencesTests
{
    private static UserPreferences CreatePreferences() => new(
        new UserId("u-42"),
        new Dictionary<WeatherType, TrackQuery>
        {
            [WeatherType.Rain] = new TrackQuery("Riders on the Storm"),
        },
        new TrackQuery("Here Comes the Sun"),
        new ChannelId("email"),
        [new ContactPoint(new ChannelId("email"), "user@example.com")]);

    [Fact]
    public void FindContactPoint_KnownChannel_ReturnsContactPoint()
    {
        var preferences = CreatePreferences();

        var contactPoint = preferences.FindContactPoint(new ChannelId("email"));

        contactPoint.ShouldNotBeNull();
        contactPoint.Address.ShouldBe("user@example.com");
    }

    [Fact]
    public void FindContactPoint_UnknownChannel_ReturnsNull()
    {
        var preferences = CreatePreferences();

        var contactPoint = preferences.FindContactPoint(new ChannelId("sms"));

        contactPoint.ShouldBeNull();
    }
}
