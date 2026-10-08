using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ReveilMusical.Infrastructure.Notifications;

namespace ReveilMusical.Infrastructure.Tests;

public class FileAndConsoleOutboxTests : IDisposable
{
    private readonly string _outboxDirectory = Path.Combine(Path.GetTempPath(), $"reveil-musical-outbox-{Guid.NewGuid():N}");

    private FileAndConsoleOutbox CreateOutbox() => new(
        Options.Create(new NotificationsOptions { OutboxDirectory = _outboxDirectory }),
        TimeProvider.System,
        NullLogger<FileAndConsoleOutbox>.Instance);

    [Fact]
    public void Write_AppendsLineToChannelSpecificFile()
    {
        var outbox = CreateOutbox();

        outbox.Write("email", "hello world");

        var filePath = Path.Combine(_outboxDirectory, "email.log");
        File.Exists(filePath).ShouldBeTrue();
        File.ReadAllText(filePath).ShouldContain("hello world");
    }

    [Fact]
    public void Write_CalledTwiceForSameChannel_AppendsBothLines()
    {
        var outbox = CreateOutbox();

        outbox.Write("sms", "first");
        outbox.Write("sms", "second");

        var content = File.ReadAllText(Path.Combine(_outboxDirectory, "sms.log"));
        content.ShouldContain("first");
        content.ShouldContain("second");
    }

    public void Dispose()
    {
        if (Directory.Exists(_outboxDirectory))
        {
            Directory.Delete(_outboxDirectory, recursive: true);
        }
    }
}
