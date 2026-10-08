namespace ReveilMusical.Application;

public sealed class UserPreferencesUnavailableException(string message, Exception? innerException = null)
    : Exception(message, innerException);
