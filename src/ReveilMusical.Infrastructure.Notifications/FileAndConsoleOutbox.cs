using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReveilMusical.Application;

namespace ReveilMusical.Infrastructure.Notifications;

internal sealed class FileAndConsoleOutbox : IOutbox
{
    private readonly NotificationsOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<FileAndConsoleOutbox> _logger;

    public FileAndConsoleOutbox(IOptions<NotificationsOptions> options, TimeProvider timeProvider, ILogger<FileAndConsoleOutbox> logger)
    {
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public void Write(string channel, string content)
    {
        _logger.LogInformation("[outbox:{Channel}] {Content}", channel, content);

        Directory.CreateDirectory(_options.OutboxDirectory);
        var path = Path.Combine(_options.OutboxDirectory, $"{channel}.log");
        File.AppendAllText(path, $"{_timeProvider.GetUtcNow():O} {content}{Environment.NewLine}");
    }
}
