using ReveilMusical.Domain;

namespace ReveilMusical.Application;

public sealed record PreferencesLookupResult(UserPreferences? Preferences, bool Degraded, string? DegradationReason);
