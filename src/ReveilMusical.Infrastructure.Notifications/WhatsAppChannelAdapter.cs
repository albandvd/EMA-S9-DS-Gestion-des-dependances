using ReveilMusical.Application;
using ReveilMusical.Domain;

namespace ReveilMusical.Infrastructure.Notifications;

internal sealed class WhatsAppChannelAdapter : INotificationChannel
{
    private readonly FakeWhatsAppClient _client;

    public WhatsAppChannelAdapter(FakeWhatsAppClient client)
    {
        _client = client;
    }

    public ChannelId Id { get; } = new("whatsapp");

    public Task SendAsync(WakeUpMessage message, ContactPoint contactPoint, CancellationToken cancellationToken)
    {
        var status = _client.Send(contactPoint.Address, message.Text);
        if (status != WhatsAppDeliveryStatus.Delivered)
        {
            throw new InvalidOperationException("WhatsApp message delivery failed.");
        }

        return Task.CompletedTask;
    }
}
