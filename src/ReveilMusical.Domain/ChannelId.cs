namespace ReveilMusical.Domain;

public sealed record ChannelId
{
    public string Value { get; }

    public ChannelId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Channel id must not be empty.", nameof(value));
        }

        Value = value.Trim().ToLowerInvariant();
    }

    public override string ToString() => Value;
}
