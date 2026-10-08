using System.ComponentModel.DataAnnotations;

namespace ReveilMusical.Application;

public sealed class NotificationDispatcherOptions
{
    public const string SectionName = "Notifications";

    [MinLength(1)]
    public IReadOnlyList<string> FallbackOrder { get; set; } = [];
}
