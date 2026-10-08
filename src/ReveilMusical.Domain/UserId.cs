namespace ReveilMusical.Domain;

public sealed record UserId
{
    public string Value { get; }

    public UserId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("User id must not be empty.", nameof(value));
        }

        Value = value;
    }

    public override string ToString() => Value;
}
