using Microsoft.Extensions.Options;
using ReveilMusical.Application;

namespace ReveilMusical.Infrastructure.Notifications;

internal sealed record PushNotification(string Title, string Body);

internal sealed class FakePushService
{
    private readonly ChannelSimulationOptions _options;
    private readonly IOutbox _outbox;

    public FakePushService(IOptions<NotificationsOptions> options, IOutbox outbox)
    {
        _options = options.Value.Push;
        _outbox = outbox;
    }

    public bool Notify(string deviceToken, PushNotification notification)
    {
        if (_options.SimulateFailure)
        {
            return false;
        }

        _outbox.Write("push", $"device={deviceToken}; title={notification.Title}; body={notification.Body}");
        return true;
    }
}
