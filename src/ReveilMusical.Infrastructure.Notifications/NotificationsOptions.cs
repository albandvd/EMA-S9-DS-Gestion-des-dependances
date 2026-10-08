namespace ReveilMusical.Infrastructure.Notifications;

internal sealed class NotificationsOptions
{
    public const string SectionName = "Notifications";

    public string OutboxDirectory { get; set; } = "outbox";

    public ChannelSimulationOptions Email { get; set; } = new();

    public ChannelSimulationOptions Sms { get; set; } = new();

    public ChannelSimulationOptions Push { get; set; } = new();
}

internal sealed class ChannelSimulationOptions
{
    public bool SimulateFailure { get; set; }
}
