using ReveilMusical.Application;
using ReveilMusical.Domain;

namespace ReveilMusical.Infrastructure.Notifications;

internal sealed class PushChannelAdapter : INotificationChannel
{
    private readonly FakePushService _service;

    public PushChannelAdapter(FakePushService service)
    {
        _service = service;
    }

    public ChannelId Id { get; } = new("push");

    public Task SendAsync(WakeUpMessage message, ContactPoint contactPoint, CancellationToken cancellationToken)
    {
        var success = _service.Notify(contactPoint.Address, new PushNotification("Réveil musical", message.Text));
        if (!success)
        {
            throw new InvalidOperationException("Push notification delivery failed.");
        }

        return Task.CompletedTask;
    }
}
