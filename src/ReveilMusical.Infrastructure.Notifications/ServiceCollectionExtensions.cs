using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ReveilMusical.Application;

namespace ReveilMusical.Infrastructure.Notifications;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationChannels(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);

        services
            .AddOptions<NotificationsOptions>()
            .Bind(configuration.GetSection(NotificationsOptions.SectionName))
            .ValidateOnStart();

        services
            .AddOptions<NotificationDispatcherOptions>()
            .Bind(configuration.GetSection(NotificationDispatcherOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IOutbox, FileAndConsoleOutbox>();

        services.AddSingleton<FakeEmailClient>();
        services.AddSingleton<FakeSmsGateway>();
        services.AddSingleton<FakePushService>();

        services.AddSingleton<INotificationChannel, EmailChannelAdapter>();
        services.AddSingleton<INotificationChannel, SmsChannelAdapter>();
        services.AddSingleton<INotificationChannel, PushChannelAdapter>();

        services.AddSingleton<INotificationDispatcher, NotificationDispatcher>();

        return services;
    }
}
