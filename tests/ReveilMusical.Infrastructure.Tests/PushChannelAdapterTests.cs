using Microsoft.Extensions.Options;
using ReveilMusical.Application;
using ReveilMusical.Domain;
using ReveilMusical.Infrastructure.Notifications;

namespace ReveilMusical.Infrastructure.Tests;

public class PushChannelAdapterTests
{
    private static readonly WakeUpMessage AnyMessage = new(
        new UserId("u-42"), new Track("Here Comes the Sun", "The Beatles"), DayOfWeek.Monday, WeatherType.Sunny, "Bon lundi !");
    private static readonly ContactPoint AnyContactPoint = new(new ChannelId("push"), "device-token");

    private static PushChannelAdapter CreateAdapter(bool simulateFailure, IOutbox outbox) => new(
        new FakePushService(
            Options.Create(new NotificationsOptions { Push = new ChannelSimulationOptions { SimulateFailure = simulateFailure } }),
            outbox));

    [Fact]
    public async Task SendAsync_ServiceSucceeds_DoesNotThrow()
    {
        var adapter = CreateAdapter(simulateFailure: false, Substitute.For<IOutbox>());

        await adapter.SendAsync(AnyMessage, AnyContactPoint, CancellationToken.None);
    }

    [Fact]
    public async Task SendAsync_ServiceReturnsFalse_ThrowsInvalidOperationException()
    {
        var adapter = CreateAdapter(simulateFailure: true, Substitute.For<IOutbox>());

        await Should.ThrowAsync<InvalidOperationException>(
            () => adapter.SendAsync(AnyMessage, AnyContactPoint, CancellationToken.None));
    }
}
