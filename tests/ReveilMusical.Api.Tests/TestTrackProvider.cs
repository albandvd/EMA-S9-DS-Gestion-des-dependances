using ReveilMusical.Application;
using ReveilMusical.Domain;

namespace ReveilMusical.Api.Tests;

internal sealed class TestTrackProvider(Track? result, Exception? failure = null) : ITrackProvider
{
    public Task<Track?> FindAsync(TrackQuery query, CancellationToken cancellationToken) =>
        failure is not null ? Task.FromException<Track?>(failure) : Task.FromResult(result);
}
