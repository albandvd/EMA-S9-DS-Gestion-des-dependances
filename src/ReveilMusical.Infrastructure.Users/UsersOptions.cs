using System.ComponentModel.DataAnnotations;

namespace ReveilMusical.Infrastructure.Users;

internal sealed class UsersOptions
{
    public const string SectionName = "Users";

    public bool SimulateFailure { get; set; }

    public List<UserProfileOptions> Profiles { get; set; } = [];
}

internal sealed class UserProfileOptions
{
    [Required]
    public string UserId { get; set; } = "";

    public Dictionary<string, string> TracksByWeather { get; set; } = [];

    [Required]
    public string FallbackTrack { get; set; } = "";

    [Required]
    public string PreferredChannel { get; set; } = "";

    public List<ContactPointOptions> ContactPoints { get; set; } = [];
}

internal sealed class ContactPointOptions
{
    [Required]
    public string Channel { get; set; } = "";

    [Required]
    public string Address { get; set; } = "";
}
