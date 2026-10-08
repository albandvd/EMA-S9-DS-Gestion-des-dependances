using ReveilMusical.Application;
using ReveilMusical.Domain;

namespace ReveilMusical.Infrastructure.Notifications;

internal sealed class EmailChannelAdapter : INotificationChannel
{
    private readonly FakeEmailClient _client;

    public EmailChannelAdapter(FakeEmailClient client)
    {
        _client = client;
    }

    public ChannelId Id { get; } = new("email");

    public Task SendAsync(WakeUpMessage message, ContactPoint contactPoint, CancellationToken cancellationToken)
    {
        _client.SendMail(contactPoint.Address, "Réveil musical", $"<p>{message.Text}</p>");
        return Task.CompletedTask;
    }
}
