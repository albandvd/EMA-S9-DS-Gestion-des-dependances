using Microsoft.Extensions.Options;
using ReveilMusical.Application;
using ReveilMusical.Domain;
using ReveilMusical.Infrastructure.Notifications;

namespace ReveilMusical.Infrastructure.Tests;

public class PushChannelAdapterContractTests : NotificationChannelContractTests
{
    protected override INotificationChannel CreateChannel(bool simulateFailure) => new PushChannelAdapter(
        new FakePushService(
            Options.Create(new NotificationsOptions { Push = new ChannelSimulationOptions { SimulateFailure = simulateFailure } }),
            Substitute.For<IOutbox>()));

    protected override ContactPoint CreateContactPoint() => new(new ChannelId("push"), "device-token");
}
