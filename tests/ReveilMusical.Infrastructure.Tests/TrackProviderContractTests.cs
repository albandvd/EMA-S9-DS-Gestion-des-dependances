using ReveilMusical.Application;
using ReveilMusical.Domain;

namespace ReveilMusical.Infrastructure.Tests;

public abstract class TrackProviderContractTests
{
    protected abstract ITrackProvider CreateProviderReturningATrack();

    [Fact]
    public virtual async Task FindAsync_WhenItReturnsATrack_TitleAndArtistAreNeverEmpty()
    {
        var provider = CreateProviderReturningATrack();

        var track = await provider.FindAsync(new TrackQuery("Here Comes the Sun"), CancellationToken.None);

        if (track is not null)
        {
            track.Title.ShouldNotBeNullOrWhiteSpace();
            track.Artist.ShouldNotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public virtual async Task FindAsync_AlreadyCancelledToken_ThrowsOperationCanceledException()
    {
        var provider = CreateProviderReturningATrack();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(
            () => provider.FindAsync(new TrackQuery("anything"), cts.Token));
    }
}
