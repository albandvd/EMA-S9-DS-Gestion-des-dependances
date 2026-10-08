using Microsoft.Extensions.Options;
using ReveilMusical.Application;
using ReveilMusical.Infrastructure.Notifications;

namespace ReveilMusical.Infrastructure.Tests;

public class FakeWhatsAppClientTests
{
    private readonly IOutbox _outbox = Substitute.For<IOutbox>();

    private FakeWhatsAppClient CreateClient(bool simulateFailure) => new(
        Options.Create(new NotificationsOptions { WhatsApp = new ChannelSimulationOptions { SimulateFailure = simulateFailure } }),
        _outbox);

    [Fact]
    public void Send_NotSimulatingFailure_ReturnsDeliveredAndWritesToOutbox()
    {
        var client = CreateClient(simulateFailure: false);

        var status = client.Send("+33600000000", "Bon lundi !");

        status.ShouldBe(WhatsAppDeliveryStatus.Delivered);
        _outbox.Received(1).Write("whatsapp", Arg.Any<string>());
    }

    [Fact]
    public void Send_SimulatingFailure_ReturnsFailedWithoutWritingToOutbox()
    {
        var client = CreateClient(simulateFailure: true);

        var status = client.Send("+33600000000", "Bon lundi !");

        status.ShouldBe(WhatsAppDeliveryStatus.Failed);
        _outbox.DidNotReceive().Write(Arg.Any<string>(), Arg.Any<string>());
    }
}
