using ReveilMusical.Domain;

namespace ReveilMusical.Application;

public interface IUserPreferencesProvider
{
    Task<PreferencesLookupResult> GetAsync(UserId userId, CancellationToken cancellationToken);
}
