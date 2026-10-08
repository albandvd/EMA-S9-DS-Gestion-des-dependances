using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReveilMusical.Application;
using ReveilMusical.Infrastructure.Notifications;

namespace ReveilMusical.Infrastructure.Tests;

public class AddNotificationChannelsTests
{
    [Fact]
    public void AddNotificationChannels_RegistersDispatcherAndAllThreeChannels()
    {
        var configurationRoot = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Notifications:FallbackOrder:0"] = "push",
                ["Notifications:FallbackOrder:1"] = "sms",
                ["Notifications:FallbackOrder:2"] = "email",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNotificationChannels(configurationRoot);
        using var provider = services.BuildServiceProvider();

        var dispatcher = provider.GetRequiredService<INotificationDispatcher>();
        var channels = provider.GetServices<INotificationChannel>().ToList();

        dispatcher.ShouldNotBeNull();
        channels.Select(c => c.Id.Value).ShouldBe(["email", "sms", "push"], ignoreOrder: true);
    }
}
