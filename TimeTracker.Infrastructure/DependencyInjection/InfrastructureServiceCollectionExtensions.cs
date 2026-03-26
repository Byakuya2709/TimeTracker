using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TimeTracker.Domain.Interfaces;
using TimeTracker.Infrastructure.Persistence;

namespace TimeTracker.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddTimeTrackerInfrastructure(this IServiceCollection services, string databasePath)
    {
        services.AddDbContextFactory<TimeTrackerDbContext>(options =>
            options.UseSqlite($"Data Source={databasePath}"));

        services.AddSingleton<ITrackingSessionRepository, SqliteActivityLogStore>();
        services.AddSingleton<IUserSettingsRepository, SqliteUserSettingsStore>();

        return services;
    }

    public static void EnsureTimeTrackerDatabaseCreated(this IServiceProvider serviceProvider)
    {
        using TimeTrackerDbContext dbContext = serviceProvider
            .GetRequiredService<IDbContextFactory<TimeTrackerDbContext>>()
            .CreateDbContext();

        dbContext.Database.EnsureCreated();
    }
}
