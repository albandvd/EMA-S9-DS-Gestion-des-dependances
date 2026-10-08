using Microsoft.Extensions.Options;
using ReveilMusical.Application;
using ReveilMusical.Domain;
using ReveilMusical.Infrastructure.Notifications;

namespace ReveilMusical.Infrastructure.Tests;

public class SmsChannelAdapterTests
{
    private static readonly ContactPoint AnyContactPoint = new(new ChannelId("sms"), "+33600000000");

    private static SmsChannelAdapter CreateAdapter(bool simulateFailure, IOutbox outbox) => new(
        new FakeSmsGateway(
            Options.Create(new NotificationsOptions { Sms = new ChannelSimulationOptions { SimulateFailure = simulateFailure } }),
            outbox));

    private static WakeUpMessage CreateMessage(string text) =>
        new(new UserId("u-42"), new Track("Here Comes the Sun", "The Beatles"), DayOfWeek.Monday, WeatherType.Sunny, text);

    [Fact]
    public async Task SendAsync_TextLongerThan160Characters_TruncatesBeforeSending()
    {
        var outbox = Substitute.For<IOutbox>();
        var adapter = CreateAdapter(simulateFailure: false, outbox);
        var longText = new string('a', 200);

        await adapter.SendAsync(CreateMessage(longText), AnyContactPoint, CancellationToken.None);

        outbox.Received(1).Write("sms", Arg.Is<string>(s => !s.Contains(new string('a', 161))));
    }

    [Fact]
    public async Task SendAsync_TextWithin160Characters_SendsUnchanged()
    {
        var outbox = Substitute.For<IOutbox>();
        var adapter = CreateAdapter(simulateFailure: false, outbox);
        const string shortText = "Bon lundi !";

        await adapter.SendAsync(CreateMessage(shortText), AnyContactPoint, CancellationToken.None);

        outbox.Received(1).Write("sms", Arg.Is<string>(s => s.Contains(shortText)));
    }

    [Fact]
    public async Task SendAsync_ReceiptReportsFailure_ThrowsInvalidOperationException()
    {
        var adapter = CreateAdapter(simulateFailure: true, Substitute.For<IOutbox>());

        await Should.ThrowAsync<InvalidOperationException>(
            () => adapter.SendAsync(CreateMessage("Bon lundi !"), AnyContactPoint, CancellationToken.None));
    }
}
