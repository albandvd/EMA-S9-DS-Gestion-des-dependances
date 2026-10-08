using Microsoft.Extensions.Options;
using ReveilMusical.Application;
using ReveilMusical.Domain;
using ReveilMusical.Infrastructure.Notifications;

namespace ReveilMusical.Infrastructure.Tests;

public class EmailChannelAdapterTests
{
    private static readonly WakeUpMessage AnyMessage = new(
        new UserId("u-42"), new Track("Here Comes the Sun", "The Beatles"), DayOfWeek.Monday, WeatherType.Sunny, "Bon lundi !");
    private static readonly ContactPoint AnyContactPoint = new(new ChannelId("email"), "user@example.com");

    private static EmailChannelAdapter CreateAdapter(bool simulateFailure, IOutbox outbox) => new(
        new FakeEmailClient(
            Options.Create(new NotificationsOptions { Email = new ChannelSimulationOptions { SimulateFailure = simulateFailure } }),
            outbox));

    [Fact]
    public async Task SendAsync_Success_WritesHtmlFormattedBodyToOutbox()
    {
        var outbox = Substitute.For<IOutbox>();
        var adapter = CreateAdapter(simulateFailure: false, outbox);

        await adapter.SendAsync(AnyMessage, AnyContactPoint, CancellationToken.None);

        outbox.Received(1).Write("email", Arg.Is<string>(s => s.Contains("<p>") && s.Contains(AnyMessage.Text)));
    }

    [Fact]
    public async Task SendAsync_SimulatedFailure_Throws()
    {
        var adapter = CreateAdapter(simulateFailure: true, Substitute.For<IOutbox>());

        await Should.ThrowAsync<InvalidOperationException>(
            () => adapter.SendAsync(AnyMessage, AnyContactPoint, CancellationToken.None));
    }
}
