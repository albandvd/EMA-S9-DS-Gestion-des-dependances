using ReveilMusical.Domain;

namespace ReveilMusical.Application;

public interface INotificationChannel
{
    ChannelId Id { get; }

    Task SendAsync(WakeUpMessage message, ContactPoint contactPoint, CancellationToken cancellationToken);
}
