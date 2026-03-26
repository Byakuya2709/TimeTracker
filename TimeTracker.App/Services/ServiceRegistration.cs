using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TimeTracker.Application.Abstractions;
using TimeTracker.Application.Services;
using TimeTracker.Domain.Interfaces;
using TimeTracker.Infrastructure.Persistence;
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
        using TimeTrackerDbContext dbContext = serviceProvider
            .GetRequiredService<IDbContextFactory<TimeTrackerDbContext>>()
            .CreateDbContext();

        dbContext.Database.EnsureCreated();
    }

    private static IServiceCollection AddTimeTrackerServices(this IServiceCollection services, string databasePath)
    {
        services.AddDbContextFactory<TimeTrackerDbContext>(options =>
            options.UseSqlite($"Data Source={databasePath}"));

        services.AddSingleton<ITrackingSessionRepository, SqliteActivityLogStore>();
        services.AddSingleton<IUserSettingsRepository, SqliteUserSettingsStore>();
        services.AddSingleton<IUserSettingsService, UserSettingsService>();
        services.AddSingleton<IIdleThresholdResolver, IdleThresholdResolver>();
        services.AddSingleton(provider =>
        {
            IIdleThresholdResolver idleThresholdResolver = provider.GetRequiredService<IIdleThresholdResolver>();
            TimeSpan idleThreshold = idleThresholdResolver.Resolve();

            return new ActivityTracker(
                new Win32ActiveAppReader(idleThreshold),
                provider.GetRequiredService<ITrackingSessionRepository>());
        });

        services.AddSingleton<MainViewModel>();
        services.AddSingleton(provider => new MainWindow(provider.GetRequiredService<MainViewModel>()));
        services.AddSingleton(provider => new OverlayWindow(provider.GetRequiredService<MainViewModel>()));

        return services;
    }
}
