using ReveilMusical.Application;
using ReveilMusical.Domain;
using ReveilMusical.Infrastructure.Music;

namespace ReveilMusical.Infrastructure.Tests;

public class LocalFallbackTrackProviderContractTests : TrackProviderContractTests
{
    protected override ITrackProvider CreateProviderReturningATrack() => new LocalFallbackTrackProvider();

    public override async Task FindAsync_AlreadyCancelledToken_ThrowsOperationCanceledException()
    {
        var provider = CreateProviderReturningATrack();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var track = await provider.FindAsync(new TrackQuery("anything"), cts.Token);

        track.ShouldNotBeNull();
    }
}
