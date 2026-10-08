using ReveilMusical.Domain;

namespace ReveilMusical.Application;

public sealed record ChannelDispatchResult(ChannelId ChannelUsed, bool Degraded, IReadOnlyList<string> Reasons);
