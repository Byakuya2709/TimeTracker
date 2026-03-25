using System.Windows.Input;

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
            if (SetProperty(ref _idleDetectionMinutes, value))
            {
                OnPropertyChanged(nameof(IsIdleDetection5Selected));
                OnPropertyChanged(nameof(IsIdleDetection10Selected));
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
        _setIdleDetection5Command = new RelayCommand(() => IdleDetectionMinutes = 5);
        _setIdleDetection10Command = new RelayCommand(() => IdleDetectionMinutes = 10);
        _toggleAutoStartCommand = new RelayCommand(() => AutoStartOnBoot = !AutoStartOnBoot);
        _setOverlayPositionCommand = new RelayCommand<OverlayAnchor>(SetOverlayPosition);
    }

    private void SetOverlayPosition(OverlayAnchor anchor)
    {
        OverlayPosition = anchor;
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
