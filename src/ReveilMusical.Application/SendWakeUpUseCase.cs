using Microsoft.Extensions.Logging;
using ReveilMusical.Domain;

namespace ReveilMusical.Application;

public sealed class SendWakeUpUseCase
{
    private readonly IUserPreferencesProvider _preferencesProvider;
    private readonly ITrackSelectionPolicy _selectionPolicy;
    private readonly ITrackResolver _trackResolver;
    private readonly INotificationDispatcher _notificationDispatcher;
    private readonly IWakeUpMessageComposer _messageComposer;
    private readonly ILogger<SendWakeUpUseCase> _logger;

    public SendWakeUpUseCase(
        IUserPreferencesProvider preferencesProvider,
        ITrackSelectionPolicy selectionPolicy,
        ITrackResolver trackResolver,
        INotificationDispatcher notificationDispatcher,
        IWakeUpMessageComposer messageComposer,
        ILogger<SendWakeUpUseCase> logger)
    {
        _preferencesProvider = preferencesProvider;
        _selectionPolicy = selectionPolicy;
        _trackResolver = trackResolver;
        _notificationDispatcher = notificationDispatcher;
        _messageComposer = messageComposer;
        _logger = logger;
    }

    public async Task<WakeUpOutcome?> ExecuteAsync(
        UserId userId,
        DayOfWeek day,
        WeatherType weather,
        CancellationToken cancellationToken)
    {
        var lookup = await _preferencesProvider.GetAsync(userId, cancellationToken);
        if (lookup.Preferences is null)
        {
            return null;
        }

        var preferences = lookup.Preferences;
        var reasons = new List<string>();
        var musicDegraded = false;

        if (lookup.Degraded)
        {
            var reason = lookup.DegradationReason ?? "User preferences served from a stale cache.";
            _logger.LogWarning("User preferences degraded for {UserId}: {Reason}", userId, reason);
            reasons.Add(reason);
        }

        var trackQuery = _selectionPolicy.Select(preferences, day, weather);
        Track track;

        try
        {
            var resolution = await _trackResolver.ResolveAsync(trackQuery, cancellationToken);
            track = resolution.Track;
            musicDegraded = resolution.Degraded;
            if (resolution.Degraded)
            {
                var reason = $"Track resolver fell back to a degraded result for query '{trackQuery}'.";
                _logger.LogWarning("Music degraded for {UserId}: {Reason}", userId, reason);
                reasons.Add(reason);
            }
        }
        catch (Exception exception)
        {
            var reason = $"Track resolver threw ({exception.GetType().Name}); using the user's raw fallback track.";
            _logger.LogWarning(exception, "Music resolution failed entirely for {UserId}: {Reason}", userId, reason);
            track = new Track(preferences.FallbackTrack.SearchText, "Artiste inconnu");
            musicDegraded = true;
            reasons.Add(reason);
        }

        var message = _messageComposer.Compose(userId, track, day, weather, musicDegraded);
        var dispatchResult = await _notificationDispatcher.DispatchAsync(message, preferences, cancellationToken);

        if (dispatchResult.Degraded)
        {
            _logger.LogWarning(
                "Notification degraded for {UserId}, channel used {ChannelUsed}: {Reasons}",
                userId,
                dispatchResult.ChannelUsed,
                string.Join("; ", dispatchResult.Reasons));
        }

        reasons.AddRange(dispatchResult.Reasons);

        return new WakeUpOutcome(track, dispatchResult.ChannelUsed, musicDegraded, dispatchResult.Degraded, reasons);
    }
}
