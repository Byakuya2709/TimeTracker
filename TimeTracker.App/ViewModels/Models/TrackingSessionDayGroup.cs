using System.Collections.ObjectModel;

namespace TimeTracker.App.ViewModels;

public class TrackingSessionDayGroup
{
    public DateOnly SessionDate { get; init; }

    public string DateTitle { get; init; } = string.Empty;

    public ObservableCollection<TrackingSessionListItem> Sessions { get; init; } = [];
}
