using Microsoft.Extensions.Options;
using ReveilMusical.Application;
using ReveilMusical.Infrastructure.Notifications;

namespace ReveilMusical.Infrastructure.Tests;

public class FakeEmailClientTests
{
    private readonly IOutbox _outbox = Substitute.For<IOutbox>();

    private FakeEmailClient CreateClient(bool simulateFailure) => new(
        Options.Create(new NotificationsOptions { Email = new ChannelSimulationOptions { SimulateFailure = simulateFailure } }),
        _outbox);

    [Fact]
    public void SendMail_NotSimulatingFailure_WritesToOutbox()
    {
        var client = CreateClient(simulateFailure: false);

        client.SendMail("user@example.com", "Bon lundi !", "<p>Bon lundi !</p>");

        _outbox.Received(1).Write("email", Arg.Is<string>(s => s.Contains("user@example.com")));
    }

    [Fact]
    public void SendMail_SimulatingFailure_ThrowsAndDoesNotWriteToOutbox()
    {
        var client = CreateClient(simulateFailure: true);

        Should.Throw<InvalidOperationException>(() => client.SendMail("user@example.com", "subject", "body"));
        _outbox.DidNotReceive().Write(Arg.Any<string>(), Arg.Any<string>());
    }
}
