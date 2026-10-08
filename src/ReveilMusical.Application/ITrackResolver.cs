using ReveilMusical.Domain;

namespace ReveilMusical.Application;

public interface ITrackResolver
{
    Task<TrackResolution> ResolveAsync(TrackQuery query, CancellationToken cancellationToken);
}
