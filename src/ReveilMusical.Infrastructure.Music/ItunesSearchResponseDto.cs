using System.Text.Json.Serialization;

namespace ReveilMusical.Infrastructure.Music;

internal sealed class ItunesSearchResponseDto
{
    [JsonPropertyName("resultCount")]
    public int ResultCount { get; set; }

    [JsonPropertyName("results")]
    public List<ItunesTrackDto> Results { get; set; } = [];
}

internal sealed class ItunesTrackDto
{
    [JsonPropertyName("trackName")]
    public string? TrackName { get; set; }

    [JsonPropertyName("artistName")]
    public string? ArtistName { get; set; }

    [JsonPropertyName("trackViewUrl")]
    public string? TrackViewUrl { get; set; }
}
