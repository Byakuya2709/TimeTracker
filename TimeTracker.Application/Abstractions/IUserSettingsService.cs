using TimeTracker.Application.Models;

namespace TimeTracker.Application.Abstractions;

public interface IUserSettingsService
{
    UserSettingsModel GetUserSettings();

    void SaveUserSettings(UserSettingsModel settings);

    TimeSpan ResolveIdleThreshold();
}
