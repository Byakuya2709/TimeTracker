using TimeTracker.Domain.Entities;

namespace TimeTracker.Domain.Interfaces;

public interface IUserSettingsRepository
{
    UserSettings GetUserSettings();

    void SaveUserSettings(UserSettings settings);
}
