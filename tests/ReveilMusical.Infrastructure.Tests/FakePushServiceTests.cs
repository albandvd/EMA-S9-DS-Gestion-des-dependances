using Microsoft.Extensions.Options;
using ReveilMusical.Application;
using ReveilMusical.Infrastructure.Notifications;

namespace ReveilMusical.Infrastructure.Tests;

public class FakePushServiceTests
{
    private readonly IOutbox _outbox = Substitute.For<IOutbox>();

    private FakePushService CreateService(bool simulateFailure) => new(
        Options.Create(new NotificationsOptions { Push = new ChannelSimulationOptions { SimulateFailure = simulateFailure } }),
        _outbox);

    [Fact]
    public void Notify_NotSimulatingFailure_ReturnsTrueAndWritesToOutbox()
    {
        var service = CreateService(simulateFailure: false);

        var result = service.Notify("device-token", new PushNotification("Bon lundi !", "On se réveille"));

        result.ShouldBeTrue();
        _outbox.Received(1).Write("push", Arg.Any<string>());
    }

    [Fact]
    public void Notify_SimulatingFailure_ReturnsFalseWithoutWritingToOutbox()
    {
        var service = CreateService(simulateFailure: true);

        var result = service.Notify("device-token", new PushNotification("title", "body"));

        result.ShouldBeFalse();
        _outbox.DidNotReceive().Write(Arg.Any<string>(), Arg.Any<string>());
    }
}
