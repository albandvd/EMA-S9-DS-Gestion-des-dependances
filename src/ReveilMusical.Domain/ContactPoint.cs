namespace ReveilMusical.Domain;

public sealed record ContactPoint
{
    public ChannelId Channel { get; }

    public string Address { get; }

    public ContactPoint(ChannelId channel, string address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            throw new ArgumentException("Contact point address must not be empty.", nameof(address));
        }

        Channel = channel;
        Address = address;
    }
}
