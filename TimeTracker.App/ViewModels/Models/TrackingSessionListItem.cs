using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TimeTracker.App.ViewModels;

public class TrackingSessionListItem : INotifyPropertyChanged
{
    private bool _isSelected;

    public event PropertyChangedEventHandler? PropertyChanged;

    public Guid Id { get; init; }

    public string SessionDate { get; init; } = string.Empty;

    public string SessionDateTitle { get; init; } = string.Empty;

    public string StartedAt { get; init; } = string.Empty;

    public string EndedAt { get; init; } = string.Empty;

    public string TimeRange { get; init; } = string.Empty;

    public string TotalDuration { get; init; } = string.Empty;

    public string IdleDuration { get; init; } = string.Empty;

    public string TopApps { get; init; } = string.Empty;

    public string AppBreakdown { get; init; } = string.Empty;

    public IReadOnlyList<AppUsageProgressItem> AppUsageProgressItems { get; init; } = [];

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
            {
                return;
            }

            _isSelected = value;
            OnPropertyChanged();
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class AppUsageProgressItem
{
    public string AppName { get; init; } = string.Empty;

    public string Duration { get; init; } = string.Empty;

    public double Percent { get; init; }

    public string PercentText => $"{Percent:0.#}%";
}
