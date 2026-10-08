namespace ReveilMusical.Domain;

public sealed record Track
{
    public string Title { get; }

    public string Artist { get; }

    public Track(string title, string artist)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Track title must not be empty.", nameof(title));
        }

        if (string.IsNullOrWhiteSpace(artist))
        {
            throw new ArgumentException("Track artist must not be empty.", nameof(artist));
        }

        Title = title;
        Artist = artist;
    }

    public override string ToString() => $"{Title} — {Artist}";
}
