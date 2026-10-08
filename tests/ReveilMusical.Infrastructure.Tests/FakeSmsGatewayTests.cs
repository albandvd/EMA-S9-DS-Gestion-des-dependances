using Microsoft.Extensions.Options;
using ReveilMusical.Application;
using ReveilMusical.Infrastructure.Notifications;

namespace ReveilMusical.Infrastructure.Tests;

public class FakeSmsGatewayTests
{
    private readonly IOutbox _outbox = Substitute.For<IOutbox>();

    private FakeSmsGateway CreateGateway(bool simulateFailure) => new(
        Options.Create(new NotificationsOptions { Sms = new ChannelSimulationOptions { SimulateFailure = simulateFailure } }),
        _outbox);

    [Fact]
    public async Task PushSmsAsync_NotSimulatingFailure_ReturnsSuccessAndWritesToOutbox()
    {
        var gateway = CreateGateway(simulateFailure: false);

        var receipt = await gateway.PushSmsAsync(new SmsPayload("+33600000000", "Bon lundi !"));

        receipt.Success.ShouldBeTrue();
        _outbox.Received(1).Write("sms", Arg.Any<string>());
    }

    [Fact]
    public async Task PushSmsAsync_SimulatingFailure_ReturnsFailureReceiptWithoutWritingToOutbox()
    {
        var gateway = CreateGateway(simulateFailure: true);

        var receipt = await gateway.PushSmsAsync(new SmsPayload("+33600000000", "Bon lundi !"));

        receipt.Success.ShouldBeFalse();
        receipt.Error.ShouldNotBeNull();
        _outbox.DidNotReceive().Write(Arg.Any<string>(), Arg.Any<string>());
    }
}
