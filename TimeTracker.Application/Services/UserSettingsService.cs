using TimeTracker.Application.Abstractions;
using TimeTracker.Domain.Entities;

namespace TimeTracker.Application.Services;

public class UserSettingsService : IUserSettingsService
{
    private readonly IUserSettingsStore _userSettingsStore;

    public UserSettingsService(IUserSettingsStore userSettingsStore)
    {
        _userSettingsStore = userSettingsStore;
    }

    public UserSettings GetUserSettings()
    {
        return _userSettingsStore.GetUserSettings();
    }

    public void SaveUserSettings(UserSettings settings)
    {
        _userSettingsStore.SaveUserSettings(settings);
    }
}
