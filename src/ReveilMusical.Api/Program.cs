using ReveilMusical.Api;
using ReveilMusical.Application;
using ReveilMusical.Domain;
using ReveilMusical.Infrastructure.Music;
using ReveilMusical.Infrastructure.Notifications;
using ReveilMusical.Infrastructure.Users;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReveilMusicalApplication();
builder.Services.AddMusicProviders(builder.Configuration);
builder.Services.AddNotificationChannels(builder.Configuration);
builder.Services.AddUserPreferences(builder.Configuration);

builder.Services.AddHealthChecks();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapHealthChecks("/health");

app.MapPost("/api/wake-ups", async (WakeUpRequest request, SendWakeUpUseCase useCase, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.UserId))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["userId"] = ["userId must not be empty."],
        });
    }

    if (!WakeUpRequestMapping.TryParseDay(request.DayOfWeek, out var day))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["dayOfWeek"] = [$"'{request.DayOfWeek}' is not a recognized day of week (LUNDI..DIMANCHE or MONDAY..SUNDAY)."],
        });
    }

    if (!WakeUpRequestMapping.TryParseWeather(request.Weather, out var weather))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["weather"] = [$"'{request.Weather}' is not a recognized weather type (SOLEIL, PLUIE, NEIGE, NUAGEUX)."],
        });
    }

    try
    {
        var outcome = await useCase.ExecuteAsync(new UserId(request.UserId), day, weather, cancellationToken);
        if (outcome is null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "User not found",
                detail: $"No preferences are registered for user '{request.UserId}'.");
        }

        var response = new WakeUpResponse(
            new TrackResponse(outcome.Track.Title, outcome.Track.Artist),
            outcome.ChannelUsed.Value,
            outcome.Degraded,
            outcome.DegradationReasons);
        return Results.Ok(response);
    }
    catch (UserPreferencesUnavailableException exception)
    {
        return Results.Problem(
            statusCode: StatusCodes.Status503ServiceUnavailable,
            title: "User preferences service unavailable",
            detail: exception.Message);
    }
})
.WithName("SendWakeUp");

app.Run();
