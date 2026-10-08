namespace ReveilMusical.Domain;

public sealed record WakeUpOutcome(
    Track Track,
    ChannelId ChannelUsed,
    bool MusicDegraded,
    bool ChannelDegraded,
    IReadOnlyList<string> DegradationReasons)
{
    public bool Degraded => MusicDegraded || ChannelDegraded;
}
