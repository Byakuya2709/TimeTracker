using TimeTracker.Application.Abstractions;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.Services;

public class UserSettingsService : IUserSettingsService
{
    private readonly IUserSettingsRepository _userSettingsStore;

    public UserSettingsService(IUserSettingsRepository userSettingsStore)
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
