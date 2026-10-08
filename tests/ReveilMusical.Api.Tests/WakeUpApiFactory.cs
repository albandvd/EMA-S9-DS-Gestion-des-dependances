using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ReveilMusical.Application;
using ReveilMusical.Domain;

namespace ReveilMusical.Api.Tests;

internal static class WakeUpApiFactory
{
    public static readonly Track HealthyTrack = new("Here Comes the Sun", "The Beatles");

    public static WebApplicationFactory<Program> Create(
        bool musicDegraded = false,
        bool notificationsDegraded = false,
        IEnumerable<KeyValuePair<string, string?>>? configOverrides = null)
    {
        var factory = new WebApplicationFactory<Program>();

        return factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Notifications:Email:SimulateFailure"] = notificationsDegraded ? "true" : "false",
                    ["Notifications:Sms:SimulateFailure"] = notificationsDegraded ? "true" : "false",
                    ["Notifications:Push:SimulateFailure"] = notificationsDegraded ? "true" : "false",
                });

                if (configOverrides is not null)
                {
                    configBuilder.AddInMemoryCollection(configOverrides);
                }
            });

            builder.ConfigureTestServices(services =>
            {
                var itunesResult = musicDegraded
                    ? new TestTrackProvider(null, new InvalidOperationException("Simulated iTunes failure."))
                    : new TestTrackProvider(HealthyTrack);
                var musicBrainzResult = new TestTrackProvider(null, new InvalidOperationException("Simulated MusicBrainz failure."));

                services.AddKeyedSingleton<ITrackProvider>("itunes", itunesResult);
                services.AddKeyedSingleton<ITrackProvider>("musicbrainz", musicBrainzResult);
            });
        });
    }
}
