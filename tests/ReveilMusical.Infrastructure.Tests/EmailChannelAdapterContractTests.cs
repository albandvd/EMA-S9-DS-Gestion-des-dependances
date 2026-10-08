using Microsoft.Extensions.Options;
using ReveilMusical.Application;
using ReveilMusical.Domain;
using ReveilMusical.Infrastructure.Notifications;

namespace ReveilMusical.Infrastructure.Tests;

public class EmailChannelAdapterContractTests : NotificationChannelContractTests
{
    protected override INotificationChannel CreateChannel(bool simulateFailure) => new EmailChannelAdapter(
        new FakeEmailClient(
            Options.Create(new NotificationsOptions { Email = new ChannelSimulationOptions { SimulateFailure = simulateFailure } }),
            Substitute.For<IOutbox>()));

    protected override ContactPoint CreateContactPoint() => new(new ChannelId("email"), "user@example.com");
}
