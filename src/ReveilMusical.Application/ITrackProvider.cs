using ReveilMusical.Domain;

namespace ReveilMusical.Application;

public interface ITrackProvider
{
    Task<Track?> FindAsync(TrackQuery query, CancellationToken cancellationToken);
}
