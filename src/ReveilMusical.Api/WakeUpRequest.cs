namespace ReveilMusical.Api;

public sealed record WakeUpRequest(string UserId, string DayOfWeek, string Weather);
