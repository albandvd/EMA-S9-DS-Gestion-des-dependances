using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReveilMusical.Application;
using ReveilMusical.Domain;
using ReveilMusical.Infrastructure.Music;

namespace ReveilMusical.Infrastructure.Tests;

public class AddMusicProvidersTests
{
    private static ServiceProvider BuildProvider(params (string Key, string Value)[] configuration)
    {
        var configurationRoot = new ConfigurationBuilder()
            .AddInMemoryCollection(configuration.ToDictionary(pair => pair.Key, pair => (string?)pair.Value))
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMusicProviders(configurationRoot);
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task AddMusicProviders_EmptyProvidersList_ResolvesUsingOnlyLocalProvider()
    {
        using var provider = BuildProvider(("Music:MusicBrainz:UserAgent", "ReveilMusical/1.0 ( contact@example.test )"));

        var resolver = provider.GetRequiredService<ITrackResolver>();
        var resolution = await resolver.ResolveAsync(new TrackQuery("Here Comes the Sun"), CancellationToken.None);

        resolution.Track.ShouldNotBeNull();
        resolution.Degraded.ShouldBeTrue();
    }

    [Fact]
    public void AddMusicProviders_UnknownProviderKey_FailsStartupValidation()
    {
        using var provider = BuildProvider(
            ("Music:Providers:0", "spotify"),
            ("Music:MusicBrainz:UserAgent", "ReveilMusical/1.0 ( contact@example.test )"));

        Should.Throw<OptionsValidationException>(() => provider.GetRequiredService<ITrackResolver>());
    }

    [Fact]
    public void AddMusicProviders_MissingMusicBrainzUserAgent_FailsStartupValidation()
    {
        using var provider = BuildProvider(("Music:Providers:0", "musicbrainz"));

        Should.Throw<OptionsValidationException>(() => provider.GetRequiredService<ITrackResolver>());
    }
}
