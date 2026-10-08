using System.ComponentModel.DataAnnotations;

namespace ReveilMusical.Infrastructure.Music;

internal sealed class MusicOptions
{
    public const string SectionName = "Music";

    public List<string> Providers { get; set; } = [];

    [Range(1, int.MaxValue)]
    public int GlobalTimeoutSeconds { get; set; } = 5;

    [Range(1, int.MaxValue)]
    public int CacheTtlHours { get; set; } = 24;

    public ItunesOptions Itunes { get; set; } = new();

    public MusicBrainzOptions MusicBrainz { get; set; } = new();
}

internal sealed class ItunesOptions
{
    [Required]
    public string BaseUrl { get; set; } = "https://itunes.apple.com/";

    [Range(1, int.MaxValue)]
    public int RequestsPerMinute { get; set; } = 20;

    [Range(1, int.MaxValue)]
    public int TimeoutSeconds { get; set; } = 2;
}

internal sealed class MusicBrainzOptions
{
    [Required]
    public string BaseUrl { get; set; } = "https://musicbrainz.org/ws/2/";

    [Required(ErrorMessage = "MusicBrainz requires a contact User-Agent (see https://musicbrainz.org/doc/MusicBrainz_API/Rate_Limiting).")]
    public string UserAgent { get; set; } = "";

    [Range(1, int.MaxValue)]
    public int TimeoutSeconds { get; set; } = 2;
}
