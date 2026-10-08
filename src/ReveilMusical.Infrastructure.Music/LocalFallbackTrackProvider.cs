using ReveilMusical.Application;
using ReveilMusical.Domain;

namespace ReveilMusical.Infrastructure.Music;

internal sealed class LocalFallbackTrackProvider : ITrackProvider
{
    private static readonly IReadOnlyList<Track> Catalogue =
    [
        new Track("Here Comes the Sun", "The Beatles"),
        new Track("Walking on Sunshine", "Katrina and the Waves"),
        new Track("Riders on the Storm", "The Doors"),
        new Track("Set Fire to the Rain", "Adele"),
        new Track("Let It Snow", "Dean Martin"),
        new Track("Winter", "Tori Amos"),
        new Track("Have You Ever Seen the Rain", "Creedence Clearwater Revival"),
        new Track("Both Sides, Now", "Joni Mitchell"),
    ];

    public Task<Track?> FindAsync(TrackQuery query, CancellationToken cancellationToken)
    {
        var match = Catalogue.FirstOrDefault(track =>
            track.Title.Contains(query.SearchText, StringComparison.OrdinalIgnoreCase) ||
            query.SearchText.Contains(track.Title, StringComparison.OrdinalIgnoreCase) ||
            track.Artist.Contains(query.SearchText, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult<Track?>(match ?? Catalogue[0]);
    }
}
