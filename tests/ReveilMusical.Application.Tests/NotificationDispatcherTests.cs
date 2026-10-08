using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ReveilMusical.Application;
using ReveilMusical.Domain;

namespace ReveilMusical.Application.Tests;

public class NotificationDispatcherTests
{
    private static readonly UserId AnyUserId = new("u-42");
    private static readonly Track AnyTrack = new("Riders on the Storm", "The Doors");
    private static readonly WakeUpMessage AnyMessage = new(AnyUserId, AnyTrack, DayOfWeek.Monday, WeatherType.Rain, "Bon lundi !");

    private readonly IOutbox _outbox = Substitute.For<IOutbox>();

    private static UserPreferences CreatePreferences(ChannelId preferredChannel, params ContactPoint[] contactPoints) => new(
        AnyUserId,
        new Dictionary<WeatherType, TrackQuery>(),
        new TrackQuery("fallback"),
        preferredChannel,
        contactPoints);

    private NotificationDispatcher CreateDispatcher(IEnumerable<INotificationChannel> channels, params string[] fallbackOrder) =>
        new(
            channels,
            _outbox,
            Options.Create(new NotificationDispatcherOptions { FallbackOrder = fallbackOrder }),
            NullLogger<NotificationDispatcher>.Instance);

    private static INotificationChannel CreateChannel(string id, bool succeeds = true)
    {
        var channel = Substitute.For<INotificationChannel>();
        channel.Id.Returns(new ChannelId(id));
        var sendCall = channel.SendAsync(Arg.Any<WakeUpMessage>(), Arg.Any<ContactPoint>(), Arg.Any<CancellationToken>());
        if (succeeds)
        {
            sendCall.Returns(Task.CompletedTask);
        }
        else
        {
            sendCall.Returns(Task.FromException(new InvalidOperationException("send failed")));
        }

        return channel;
    }

    [Fact]
    public async Task DispatchAsync_PreferredChannelSucceeds_ReturnsNonDegradedResult()
    {
        var email = CreateChannel("email");
        var preferences = CreatePreferences(new ChannelId("email"), new ContactPoint(new ChannelId("email"), "user@example.com"));
        var dispatcher = CreateDispatcher([email], "sms", "email");

        var result = await dispatcher.DispatchAsync(AnyMessage, preferences, CancellationToken.None);

        result.ChannelUsed.ShouldBe(new ChannelId("email"));
        result.Degraded.ShouldBeFalse();
        result.Reasons.ShouldBeEmpty();
    }

    [Fact]
    public async Task DispatchAsync_PreferredChannelFails_FallsBackToNextChannel()
    {
        var push = CreateChannel("push", succeeds: false);
        var sms = CreateChannel("sms");
        var preferences = CreatePreferences(
            new ChannelId("push"),
            new ContactPoint(new ChannelId("push"), "device-token"),
            new ContactPoint(new ChannelId("sms"), "+33600000000"));
        var dispatcher = CreateDispatcher([push, sms], "sms", "email");

        var result = await dispatcher.DispatchAsync(AnyMessage, preferences, CancellationToken.None);

        result.ChannelUsed.ShouldBe(new ChannelId("sms"));
        result.Degraded.ShouldBeTrue();
        result.Reasons.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task DispatchAsync_PreferredChannelUnknown_SkipsToFallbackOrder()
    {
        var sms = CreateChannel("sms");
        var preferences = CreatePreferences(
            new ChannelId("whatsapp"),
            new ContactPoint(new ChannelId("sms"), "+33600000000"));
        var dispatcher = CreateDispatcher([sms], "sms");

        var result = await dispatcher.DispatchAsync(AnyMessage, preferences, CancellationToken.None);

        result.ChannelUsed.ShouldBe(new ChannelId("sms"));
        result.Degraded.ShouldBeTrue();
    }

    [Fact]
    public async Task DispatchAsync_ChannelWithoutContactPoint_IsSkipped()
    {
        var sms = CreateChannel("sms");
        var preferences = CreatePreferences(
            new ChannelId("push"),
            new ContactPoint(new ChannelId("sms"), "+33600000000"));
        var dispatcher = CreateDispatcher([sms], "sms");

        var result = await dispatcher.DispatchAsync(AnyMessage, preferences, CancellationToken.None);

        result.ChannelUsed.ShouldBe(new ChannelId("sms"));
    }

    [Fact]
    public async Task DispatchAsync_AllChannelsFail_UsesLastResortAndPersistsToOutbox()
    {
        var push = CreateChannel("push", succeeds: false);
        var sms = CreateChannel("sms", succeeds: false);
        var preferences = CreatePreferences(
            new ChannelId("push"),
            new ContactPoint(new ChannelId("push"), "device-token"),
            new ContactPoint(new ChannelId("sms"), "+33600000000"));
        var dispatcher = CreateDispatcher([push, sms], "sms");

        var result = await dispatcher.DispatchAsync(AnyMessage, preferences, CancellationToken.None);

        result.ChannelUsed.ShouldBe(NotificationDispatcher.LastResortChannelId);
        result.Degraded.ShouldBeTrue();
        _outbox.Received(1).Write("undelivered", Arg.Any<string>());
    }
}
