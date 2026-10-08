namespace ReveilMusical.Domain;

public sealed record TrackQuery
{
    public string SearchText { get; }

    public TrackQuery(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            throw new ArgumentException("Track query search text must not be empty.", nameof(searchText));
        }

        SearchText = searchText;
    }

    public override string ToString() => SearchText;
}
