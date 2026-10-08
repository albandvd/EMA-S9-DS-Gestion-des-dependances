namespace ReveilMusical.Domain;

public sealed record WakeUpMessage(
    UserId Recipient,
    Track Track,
    DayOfWeek Day,
    WeatherType Weather,
    string Text);
