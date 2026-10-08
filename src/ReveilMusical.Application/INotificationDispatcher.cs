using ReveilMusical.Domain;

namespace ReveilMusical.Application;

public interface INotificationDispatcher
{
    Task<ChannelDispatchResult> DispatchAsync(
        WakeUpMessage message,
        UserPreferences preferences,
        CancellationToken cancellationToken);
}
