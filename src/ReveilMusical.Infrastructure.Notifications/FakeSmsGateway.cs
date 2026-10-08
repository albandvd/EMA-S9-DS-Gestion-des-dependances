using Microsoft.Extensions.Options;
using ReveilMusical.Application;

namespace ReveilMusical.Infrastructure.Notifications;

internal sealed record SmsPayload(string To, string Text);

internal sealed record SmsReceipt(bool Success, string? Error);

internal sealed class FakeSmsGateway
{
    private readonly ChannelSimulationOptions _options;
    private readonly IOutbox _outbox;

    public FakeSmsGateway(IOptions<NotificationsOptions> options, IOutbox outbox)
    {
        _options = options.Value.Sms;
        _outbox = outbox;
    }

    public Task<SmsReceipt> PushSmsAsync(SmsPayload payload)
    {
        if (_options.SimulateFailure)
        {
            return Task.FromResult(new SmsReceipt(Success: false, Error: "Simulated SMS gateway failure."));
        }

        _outbox.Write("sms", $"to={payload.To}; text={payload.Text}");
        return Task.FromResult(new SmsReceipt(Success: true, Error: null));
    }
}
