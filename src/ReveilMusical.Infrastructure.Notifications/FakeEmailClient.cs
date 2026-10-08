using Microsoft.Extensions.Options;
using ReveilMusical.Application;

namespace ReveilMusical.Infrastructure.Notifications;

internal sealed class FakeEmailClient
{
    private readonly ChannelSimulationOptions _options;
    private readonly IOutbox _outbox;

    public FakeEmailClient(IOptions<NotificationsOptions> options, IOutbox outbox)
    {
        _options = options.Value.Email;
        _outbox = outbox;
    }

    public void SendMail(string to, string subject, string htmlBody)
    {
        if (_options.SimulateFailure)
        {
            throw new InvalidOperationException("Simulated email delivery failure.");
        }

        _outbox.Write("email", $"to={to}; subject={subject}; body={htmlBody}");
    }
}
