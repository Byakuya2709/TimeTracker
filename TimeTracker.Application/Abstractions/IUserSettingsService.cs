using TimeTracker.Application.Models;

namespace TimeTracker.Application.Abstractions;

public interface IUserSettingsService
{
    event Action<UserSettingsModel>? SettingsChanged;

    UserSettingsModel GetUserSettings();

    void SaveUserSettings(UserSettingsModel settings);

    TimeSpan ResolveIdleThreshold();
}
