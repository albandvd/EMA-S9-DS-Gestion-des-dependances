using ReveilMusical.Application;
using ReveilMusical.Domain;

namespace ReveilMusical.Infrastructure.Notifications;

internal sealed class SmsChannelAdapter : INotificationChannel
{
    private const int MaxSmsLength = 160;

    private readonly FakeSmsGateway _gateway;

    public SmsChannelAdapter(FakeSmsGateway gateway)
    {
        _gateway = gateway;
    }

    public ChannelId Id { get; } = new("sms");

    public async Task SendAsync(WakeUpMessage message, ContactPoint contactPoint, CancellationToken cancellationToken)
    {
        var text = message.Text.Length > MaxSmsLength ? message.Text[..MaxSmsLength] : message.Text;
        var receipt = await _gateway.PushSmsAsync(new SmsPayload(contactPoint.Address, text));

        if (!receipt.Success)
        {
            throw new InvalidOperationException($"SMS delivery failed: {receipt.Error}");
        }
    }
}
