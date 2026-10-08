using Microsoft.Extensions.Options;
using ReveilMusical.Application;

namespace ReveilMusical.Infrastructure.Notifications;

internal enum WhatsAppDeliveryStatus
{
    Delivered,
    Failed,
}

internal sealed class FakeWhatsAppClient
{
    private readonly ChannelSimulationOptions _options;
    private readonly IOutbox _outbox;

    public FakeWhatsAppClient(IOptions<NotificationsOptions> options, IOutbox outbox)
    {
        _options = options.Value.WhatsApp;
        _outbox = outbox;
    }

    public WhatsAppDeliveryStatus Send(string phoneNumber, string message)
    {
        if (_options.SimulateFailure)
        {
            return WhatsAppDeliveryStatus.Failed;
        }

        _outbox.Write("whatsapp", $"to={phoneNumber}; message={message}");
        return WhatsAppDeliveryStatus.Delivered;
    }
}
