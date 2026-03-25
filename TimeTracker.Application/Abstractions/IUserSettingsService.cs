using TimeTracker.Domain.Entities;

namespace TimeTracker.Application.Abstractions;

public interface IUserSettingsService
{
    UserSettings GetUserSettings();

    void SaveUserSettings(UserSettings settings);
}
