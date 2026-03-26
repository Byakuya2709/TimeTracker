using Microsoft.Extensions.DependencyInjection;
using TimeTracker.Application.Abstractions;
using TimeTracker.Application.Services;
using TimeTracker.Domain.Interfaces;
using TimeTracker.Infrastructure.DependencyInjection;
using TimeTracker.Infrastructure.Services;
using TimeTracker.App.ViewModels;

namespace TimeTracker.App.Services;

public static class ServiceRegistration
{
    public static ServiceProvider BuildServiceProvider(string databasePath)
    {
        ServiceCollection services = new();
        services.AddTimeTrackerServices(databasePath);

        return services.BuildServiceProvider();
    }

    public static void EnsureDatabaseCreated(IServiceProvider serviceProvider)
    {
        serviceProvider.EnsureTimeTrackerDatabaseCreated();
    }

    private static IServiceCollection AddTimeTrackerServices(this IServiceCollection services, string databasePath)
    {
        services.AddTimeTrackerInfrastructure(databasePath);

        services.AddSingleton<IUserSettingsService, UserSettingsService>();
        services.AddSingleton<ITrackingSessionService, TrackingSessionService>();
        services.AddSingleton<ActivityTracker>(provider =>
        {
            IUserSettingsService userSettingsService = provider.GetRequiredService<IUserSettingsService>();
            TimeSpan idleThreshold = userSettingsService.ResolveIdleThreshold();

            return new ActivityTracker(
                new Win32ActiveAppReader(idleThreshold),
            provider.GetRequiredService<ITrackingSessionRepository>());
        });
        services.AddSingleton<ITrackingRuntimeService>(provider => provider.GetRequiredService<ActivityTracker>());

        services.AddSingleton<MainViewModel>();
        services.AddSingleton(provider => new MainWindow(provider.GetRequiredService<MainViewModel>()));
        services.AddSingleton(provider => new OverlayWindow(provider.GetRequiredService<MainViewModel>()));

        return services;
    }
}
