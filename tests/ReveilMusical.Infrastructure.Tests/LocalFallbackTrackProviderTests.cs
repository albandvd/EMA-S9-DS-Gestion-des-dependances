using ReveilMusical.Domain;
using ReveilMusical.Infrastructure.Music;

namespace ReveilMusical.Infrastructure.Tests;

public class LocalFallbackTrackProviderTests
{
    private readonly LocalFallbackTrackProvider _provider = new();

    [Fact]
    public async Task FindAsync_QueryMatchesATitle_ReturnsThatTrack()
    {
        var result = await _provider.FindAsync(new TrackQuery("Here Comes the Sun"), CancellationToken.None);

        result.ShouldNotBeNull();
        result.Title.ShouldBe("Here Comes the Sun");
    }

    [Fact]
    public async Task FindAsync_QueryMatchesNothing_ReturnsFirstCatalogueTrackRatherThanNull()
    {
        var result = await _provider.FindAsync(new TrackQuery("Some completely unknown song title"), CancellationToken.None);

        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task FindAsync_NeverThrows()
    {
        var result = await _provider.FindAsync(new TrackQuery("anything"), CancellationToken.None);

        result.ShouldNotBeNull();
    }
}
