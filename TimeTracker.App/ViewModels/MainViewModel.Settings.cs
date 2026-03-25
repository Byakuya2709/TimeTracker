using System.Windows.Input;
using TimeTracker.Domain.Entities;

namespace TimeTracker.App.ViewModels;

public partial class MainViewModel
{
    private RelayCommand _setIdleDetection5Command = null!;
    private RelayCommand _setIdleDetection10Command = null!;
    private RelayCommand _toggleAutoStartCommand = null!;
    private RelayCommand<OverlayAnchor> _setOverlayPositionCommand = null!;

    private int _idleDetectionMinutes = 10;
    private bool _autoStartOnBoot;
    private int _overlayOpacity = 85;
    private bool _isApplyingSettings;

    private OverlayAnchor _overlayPosition = OverlayAnchor.TopRight;

    public ICommand SetIdleDetection5Command => _setIdleDetection5Command;

    public ICommand SetIdleDetection10Command => _setIdleDetection10Command;

    public ICommand ToggleAutoStartCommand => _toggleAutoStartCommand;

    public ICommand SetOverlayPositionCommand => _setOverlayPositionCommand;

    public int IdleDetectionMinutes
    {
        get => _idleDetectionMinutes;
        private set
        {
            int normalized = NormalizeIdleDetectionMinutes(value);
            if (SetProperty(ref _idleDetectionMinutes, normalized))
            {
                OnPropertyChanged(nameof(IsIdleDetection5Selected));
                OnPropertyChanged(nameof(IsIdleDetection10Selected));
                PersistUserSettings();
            }
        }
    }

    public bool IsIdleDetection5Selected => IdleDetectionMinutes == 5;

    public bool IsIdleDetection10Selected => IdleDetectionMinutes == 10;

    public bool AutoStartOnBoot
    {
        get => _autoStartOnBoot;
        private set
        {
            if (SetProperty(ref _autoStartOnBoot, value))
            {
                OnPropertyChanged(nameof(AutoStartOnBootStateText));
                PersistUserSettings();
            }
        }
    }

    public string AutoStartOnBootStateText => AutoStartOnBoot ? "ON" : "OFF";

    public int OverlayOpacity
    {
        get => _overlayOpacity;
        set
        {
            int normalized = Math.Clamp(value, 10, 100);
            if (SetProperty(ref _overlayOpacity, normalized))
            {
                OnPropertyChanged(nameof(OverlayOpacityText));
                OnPropertyChanged(nameof(OverlayOpacityRatio));
                PersistUserSettings();
            }
        }
    }

    public string OverlayOpacityText => $"{OverlayOpacity}%";

    public double OverlayOpacityRatio => OverlayOpacity / 100d;

    public OverlayAnchor OverlayPosition
    {
        get => _overlayPosition;
        private set
        {
            if (SetProperty(ref _overlayPosition, value))
            {
                OnPropertyChanged(nameof(IsTopLeftOverlay));
                OnPropertyChanged(nameof(IsTopCenterOverlay));
                OnPropertyChanged(nameof(IsTopRightOverlay));
                OnPropertyChanged(nameof(IsMiddleLeftOverlay));
                OnPropertyChanged(nameof(IsCenterOverlay));
                OnPropertyChanged(nameof(IsMiddleRightOverlay));
                OnPropertyChanged(nameof(IsBottomLeftOverlay));
                OnPropertyChanged(nameof(IsBottomCenterOverlay));
                OnPropertyChanged(nameof(IsBottomRightOverlay));
                PersistUserSettings();
            }
        }
    }

    public bool IsTopLeftOverlay => OverlayPosition == OverlayAnchor.TopLeft;

    public bool IsTopCenterOverlay => OverlayPosition == OverlayAnchor.TopCenter;

    public bool IsTopRightOverlay => OverlayPosition == OverlayAnchor.TopRight;

    public bool IsMiddleLeftOverlay => OverlayPosition == OverlayAnchor.MiddleLeft;

    public bool IsCenterOverlay => OverlayPosition == OverlayAnchor.Center;

    public bool IsMiddleRightOverlay => OverlayPosition == OverlayAnchor.MiddleRight;

    public bool IsBottomLeftOverlay => OverlayPosition == OverlayAnchor.BottomLeft;

    public bool IsBottomCenterOverlay => OverlayPosition == OverlayAnchor.BottomCenter;

    public bool IsBottomRightOverlay => OverlayPosition == OverlayAnchor.BottomRight;

    private partial void InitializeSettingsPageCommands()
    {
        _setIdleDetection5Command = new RelayCommand(() => SetIdleDetectionMinutes(5));
        _setIdleDetection10Command = new RelayCommand(() => SetIdleDetectionMinutes(10));
        _toggleAutoStartCommand = new RelayCommand(() => AutoStartOnBoot = !AutoStartOnBoot);
        _setOverlayPositionCommand = new RelayCommand<OverlayAnchor>(SetOverlayPosition);
    }

    private void SetIdleDetectionMinutes(int minutes)
    {
        int normalized = NormalizeIdleDetectionMinutes(minutes);
        bool isUnchanged = IdleDetectionMinutes == normalized;

        IdleDetectionMinutes = normalized;
        ApplyIdleDetectionThreshold();

        if (isUnchanged)
        {
            PersistUserSettings();
        }
    }

    private void SetOverlayPosition(OverlayAnchor anchor)
    {
        OverlayPosition = anchor;
    }

    private void LoadUserSettings()
    {
        try
        {
            _isApplyingSettings = true;
            UserSettings settings = _userSettingsService.GetUserSettings();

            IdleDetectionMinutes = settings.IdleDetectionMinutes;
            AutoStartOnBoot = settings.AutoStartOnBoot;
            OverlayOpacity = settings.OverlayOpacity;
            OverlayPosition = ParseOverlayAnchor(settings.OverlayPosition);
            ApplyIdleDetectionThreshold();
        }
        catch
        {
            // Keep in-memory defaults when database is unavailable.
        }
        finally
        {
            _isApplyingSettings = false;
        }
    }

    private void PersistUserSettings()
    {
        if (_isApplyingSettings)
        {
            return;
        }

        try
        {
            _userSettingsService.SaveUserSettings(new UserSettings
            {
                IdleDetectionMinutes = IdleDetectionMinutes,
                AutoStartOnBoot = AutoStartOnBoot,
                OverlayOpacity = OverlayOpacity,
                OverlayPosition = OverlayPosition.ToString()
            });
        }
        catch
        {
            // UI should stay responsive even if persistence fails temporarily.
        }
    }

    private static int NormalizeIdleDetectionMinutes(int value)
    {
        return value <= 5 ? 5 : 10;
    }

    private void ApplyIdleDetectionThreshold()
    {
        _activityTracker.SetIdleThreshold(TimeSpan.FromMinutes(IdleDetectionMinutes));
    }

    private static OverlayAnchor ParseOverlayAnchor(string? value)
    {
        return Enum.TryParse(value, ignoreCase: true, out OverlayAnchor parsed)
            ? parsed
            : OverlayAnchor.TopRight;
    }
}

public enum OverlayAnchor
{
    TopLeft,
    TopCenter,
    TopRight,
    MiddleLeft,
    Center,
    MiddleRight,
    BottomLeft,
    BottomCenter,
    BottomRight
}
