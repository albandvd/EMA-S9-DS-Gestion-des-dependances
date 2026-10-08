namespace ReveilMusical.Api;

public sealed record WakeUpResponse(TrackResponse Track, string Channel, bool Degraded, IReadOnlyList<string> DegradationReasons);

public sealed record TrackResponse(string Title, string Artist);
