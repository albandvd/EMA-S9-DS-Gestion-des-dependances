using ReveilMusical.Application;
using ReveilMusical.Domain;

namespace ReveilMusical.Infrastructure.Tests;

public abstract class NotificationChannelContractTests
{
    private static readonly WakeUpMessage AnyMessage = new(
        new UserId("u-42"), new Track("Here Comes the Sun", "The Beatles"), DayOfWeek.Monday, WeatherType.Sunny, "Bon lundi !");

    protected abstract INotificationChannel CreateChannel(bool simulateFailure);

    protected abstract ContactPoint CreateContactPoint();

    [Fact]
    public void Id_IsNotNull()
    {
        CreateChannel(simulateFailure: false).Id.ShouldNotBeNull();
    }

    [Fact]
    public async Task SendAsync_HappyPath_DoesNotThrow()
    {
        var channel = CreateChannel(simulateFailure: false);

        await channel.SendAsync(AnyMessage, CreateContactPoint(), CancellationToken.None);
    }

    [Fact]
    public async Task SendAsync_SimulatedFailure_ThrowsRatherThanFailingSilently()
    {
        var channel = CreateChannel(simulateFailure: true);

        await Should.ThrowAsync<Exception>(
            () => channel.SendAsync(AnyMessage, CreateContactPoint(), CancellationToken.None));
    }
}
