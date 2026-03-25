using TimeTracker.Domain.Entities;

namespace TimeTracker.Application.Abstractions;

public interface IUserSettingsStore
{
    UserSettings GetUserSettings();

    void SaveUserSettings(UserSettings settings);
}
