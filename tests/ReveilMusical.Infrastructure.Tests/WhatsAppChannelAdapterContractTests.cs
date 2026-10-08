using Microsoft.Extensions.Options;
using ReveilMusical.Application;
using ReveilMusical.Domain;
using ReveilMusical.Infrastructure.Notifications;

namespace ReveilMusical.Infrastructure.Tests;

public class WhatsAppChannelAdapterContractTests : NotificationChannelContractTests
{
    protected override INotificationChannel CreateChannel(bool simulateFailure) => new WhatsAppChannelAdapter(
        new FakeWhatsAppClient(
            Options.Create(new NotificationsOptions { WhatsApp = new ChannelSimulationOptions { SimulateFailure = simulateFailure } }),
            Substitute.For<IOutbox>()));

    protected override ContactPoint CreateContactPoint() => new(new ChannelId("whatsapp"), "+33600000000");
}
