using Microsoft.Extensions.Options;

namespace ReveilMusical.Infrastructure.Music;

internal sealed class MusicOptionsValidation : IValidateOptions<MusicOptions>
{
    internal static readonly IReadOnlySet<string> KnownProviderKeys =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "itunes", "musicbrainz" };

    public ValidateOptionsResult Validate(string? name, MusicOptions options)
    {
        var failures = new List<string>();

        var unknownKeys = options.Providers.Where(key => !KnownProviderKeys.Contains(key)).ToList();
        if (unknownKeys.Count > 0)
        {
            failures.Add(
                $"Unknown key(s) in Music:Providers: {string.Join(", ", unknownKeys)}. " +
                $"Known keys are: {string.Join(", ", KnownProviderKeys)}. " +
                "The local fallback provider is always appended automatically and must not be listed.");
        }

        if (string.IsNullOrWhiteSpace(options.MusicBrainz.UserAgent))
        {
            failures.Add(
                "Music:MusicBrainz:UserAgent is required (MusicBrainz rejects anonymous traffic), " +
                "e.g. \"ReveilMusical/1.0 ( contact@example.com )\".");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
