using Microsoft.Extensions.Options;
using ReveilMusical.Application;
using ReveilMusical.Domain;
using ReveilMusical.Infrastructure.Notifications;

namespace ReveilMusical.Infrastructure.Tests;

public class SmsChannelAdapterContractTests : NotificationChannelContractTests
{
    protected override INotificationChannel CreateChannel(bool simulateFailure) => new SmsChannelAdapter(
        new FakeSmsGateway(
            Options.Create(new NotificationsOptions { Sms = new ChannelSimulationOptions { SimulateFailure = simulateFailure } }),
            Substitute.For<IOutbox>()));

    protected override ContactPoint CreateContactPoint() => new(new ChannelId("sms"), "+33600000000");
}
