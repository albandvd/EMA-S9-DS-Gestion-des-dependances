using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReveilMusical.Domain;

namespace ReveilMusical.Application;

public sealed class NotificationDispatcher : INotificationDispatcher
{
    public static readonly ChannelId LastResortChannelId = new("undelivered");

    private readonly IReadOnlyDictionary<ChannelId, INotificationChannel> _channelsById;
    private readonly IOutbox _outbox;
    private readonly NotificationDispatcherOptions _options;
    private readonly ILogger<NotificationDispatcher> _logger;

    public NotificationDispatcher(
        IEnumerable<INotificationChannel> channels,
        IOutbox outbox,
        IOptions<NotificationDispatcherOptions> options,
        ILogger<NotificationDispatcher> logger)
    {
        _channelsById = channels.ToDictionary(channel => channel.Id);
        _outbox = outbox;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ChannelDispatchResult> DispatchAsync(
        WakeUpMessage message,
        UserPreferences preferences,
        CancellationToken cancellationToken)
    {
        var reasons = new List<string>();
        var candidates = BuildCandidateOrder(preferences.PreferredChannel);

        foreach (var (channelId, isPreferred) in candidates)
        {
            var contactPoint = preferences.FindContactPoint(channelId);
            if (contactPoint is null)
            {
                reasons.Add($"No contact point registered for channel '{channelId}'.");
                continue;
            }

            if (!_channelsById.TryGetValue(channelId, out var channel))
            {
                reasons.Add($"Channel '{channelId}' is not registered.");
                continue;
            }

            try
            {
                await channel.SendAsync(message, contactPoint, cancellationToken);
                return new ChannelDispatchResult(channelId, Degraded: !isPreferred, reasons);
            }
            catch (Exception exception)
            {
                var reason = $"Channel '{channelId}' failed ({exception.GetType().Name}): {exception.Message}";
                _logger.LogWarning(exception, "Notification channel {ChannelId} failed for {Recipient}", channelId, message.Recipient);
                reasons.Add(reason);
            }
        }

        reasons.Add("All channels failed; message persisted to the undelivered outbox.");
        _logger.LogCritical(
            "All notification channels failed for {Recipient}: {Reasons}",
            message.Recipient,
            string.Join("; ", reasons));
        _outbox.Write(LastResortChannelId.Value, $"{message.Recipient}: {message.Text}");

        return new ChannelDispatchResult(LastResortChannelId, Degraded: true, reasons);
    }

    private IEnumerable<(ChannelId ChannelId, bool IsPreferred)> BuildCandidateOrder(ChannelId preferredChannel)
    {
        yield return (preferredChannel, true);

        foreach (var channelName in _options.FallbackOrder)
        {
            var channelId = new ChannelId(channelName);
            if (channelId != preferredChannel)
            {
                yield return (channelId, false);
            }
        }
    }
}
