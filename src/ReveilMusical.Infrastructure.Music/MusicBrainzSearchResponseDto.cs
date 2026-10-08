using System.Text.Json.Serialization;

namespace ReveilMusical.Infrastructure.Music;

internal sealed class MusicBrainzSearchResponseDto
{
    [JsonPropertyName("recordings")]
    public List<MusicBrainzRecordingDto> Recordings { get; set; } = [];
}

internal sealed class MusicBrainzRecordingDto
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("artist-credit")]
    public List<MusicBrainzArtistCreditDto> ArtistCredit { get; set; } = [];
}

internal sealed class MusicBrainzArtistCreditDto
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}
