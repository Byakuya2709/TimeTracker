using TimeTracker.Application.Abstractions;
using TimeTracker.Domain.Entities;

namespace TimeTracker.Application.Services;

public sealed class IdleThresholdResolver : IIdleThresholdResolver
{
    private const int DefaultIdleSeconds = 300;
    private const string IdleSecondsEnvironmentVariable = "TIMETRACKER_IDLE_SECONDS";

    private readonly IUserSettingsService _userSettingsService;

    public IdleThresholdResolver(IUserSettingsService userSettingsService)
    {
        _userSettingsService = userSettingsService;
    }

    public TimeSpan Resolve()
    {
        try
        {
            UserSettings settings = _userSettingsService.GetUserSettings();
            if (settings.IdleDetectionMinutes > 0)
            {
                return TimeSpan.FromMinutes(settings.IdleDetectionMinutes);
            }
        }
        catch
        {
            // Fallback to environment variable when settings table is unavailable.
        }

        return ResolveFromEnvironment();
    }

    private static TimeSpan ResolveFromEnvironment()
    {
        string? secondsValue = Environment.GetEnvironmentVariable(IdleSecondsEnvironmentVariable)
            ?? DefaultIdleSeconds.ToString();

        if (!int.TryParse(secondsValue, out int seconds) || seconds < 1)
        {
            return TimeSpan.FromSeconds(DefaultIdleSeconds);
        }

        return TimeSpan.FromSeconds(seconds);
    }
}
