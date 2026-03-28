using TimeTracker.Application.Abstractions;
using TimeTracker.Application.Models;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.Services;

public class UserSettingsService : IUserSettingsService
{
    private const int DefaultIdleSeconds = 300;
    private const string IdleSecondsEnvironmentVariable = "TIMETRACKER_IDLE_SECONDS";

    private readonly IUserSettingsRepository _userSettingsStore;

    public event Action<UserSettingsModel>? SettingsChanged;

    public UserSettingsService(IUserSettingsRepository userSettingsStore)
    {
        _userSettingsStore = userSettingsStore;
    }

    public UserSettingsModel GetUserSettings()
    {
        UserSettings settings = _userSettingsStore.GetUserSettings();

        return new UserSettingsModel
        {
            IdleDetectionMinutes = settings.IdleDetectionMinutes,
            AutoStartOnBoot = settings.AutoStartOnBoot,
            OverlayOpacity = settings.OverlayOpacity,
            OverlayPosition = settings.OverlayPosition
        };
    }

    public void SaveUserSettings(UserSettingsModel settings)
    {
        _userSettingsStore.SaveUserSettings(new UserSettings
        {
            IdleDetectionMinutes = settings.IdleDetectionMinutes,
            AutoStartOnBoot = settings.AutoStartOnBoot,
            OverlayOpacity = settings.OverlayOpacity,
            OverlayPosition = settings.OverlayPosition
        });

        SettingsChanged?.Invoke(settings);
    }

    public TimeSpan ResolveIdleThreshold()
    {
        try
        {
            UserSettingsModel settings = GetUserSettings();
            if (settings.IdleDetectionMinutes > 0)
            {
                return TimeSpan.FromMinutes(settings.IdleDetectionMinutes);
            }
        }
        catch
        {
            // Fallback to environment variable when settings table is unavailable.
        }

        return ResolveIdleThresholdFromEnvironment();
    }

    private static TimeSpan ResolveIdleThresholdFromEnvironment()
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
