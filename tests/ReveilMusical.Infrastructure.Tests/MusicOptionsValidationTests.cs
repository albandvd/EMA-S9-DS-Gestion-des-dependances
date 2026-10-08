using ReveilMusical.Infrastructure.Music;

namespace ReveilMusical.Infrastructure.Tests;

public class MusicOptionsValidationTests
{
    private const string ValidUserAgent = "ReveilMusical/1.0 ( contact@example.test )";

    private readonly MusicOptionsValidation _validation = new();

    private static MusicOptions CreateOptions(List<string> providers, string userAgent = ValidUserAgent) => new()
    {
        Providers = providers,
        MusicBrainz = new MusicBrainzOptions { UserAgent = userAgent },
    };

    [Theory]
    [InlineData("itunes")]
    [InlineData("musicbrainz")]
    [InlineData("ITUNES")]
    public void Validate_KnownProviderKey_Succeeds(string key)
    {
        var result = _validation.Validate(null, CreateOptions([key]));

        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Validate_EmptyProvidersList_Succeeds()
    {
        var result = _validation.Validate(null, CreateOptions([]));

        result.Succeeded.ShouldBeTrue();
    }

    [Theory]
    [InlineData("spotify")]
    [InlineData("local")]
    public void Validate_UnknownProviderKey_Fails(string key)
    {
        var result = _validation.Validate(null, CreateOptions([key]));

        result.Succeeded.ShouldBeFalse();
        result.FailureMessage.ShouldNotBeNull();
        result.FailureMessage.ShouldContain(key);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_MissingMusicBrainzUserAgent_Fails(string userAgent)
    {
        var result = _validation.Validate(null, CreateOptions([], userAgent));

        result.Succeeded.ShouldBeFalse();
        result.FailureMessage.ShouldNotBeNull();
        result.FailureMessage.ShouldContain("UserAgent");
    }
}
