using TimeTracker.Application.Abstractions;
using TimeTracker.Application.Models;
using TimeTracker.Application.UseCases.Tracking;

namespace TimeTracker.Application.Services;

public class ActivityTracker
{
    private readonly IActiveAppReader _activeAppReader;
    private readonly IActivityLogStore _activityLogStore;
    private readonly TrackingSessionState _sessionState = new();
    private readonly StartTrackingUseCase _startTrackingUseCase = new();
    private readonly PauseTrackingUseCase _pauseTrackingUseCase = new();
    private readonly StopTrackingUseCase _stopTrackingUseCase = new();
    private readonly TickTrackingUseCase _tickTrackingUseCase = new();
    public TrackingState State => _sessionState.State;

    public ActivityTracker(IActiveAppReader activeAppReader, IActivityLogStore activityLogStore)
    {
        _activeAppReader = activeAppReader;
        _activityLogStore = activityLogStore;
    }

    public void SetIdleThreshold(TimeSpan idleThreshold)
    {
        if (_activeAppReader is IIdleThresholdConfigurable configurableReader)
        {
            configurableReader.SetIdleThreshold(idleThreshold);
        }
    }

    public TrackingSnapshot Tick()
    {
        return _tickTrackingUseCase.Execute(_sessionState, _activeAppReader, DateTime.Now);
    }

    public void Start()
    {
        _startTrackingUseCase.Execute(_sessionState, _activeAppReader, DateTime.Now);
    }

    public void Pause()
    {
        _pauseTrackingUseCase.Execute(_sessionState, DateTime.Now);
    }


    public void Stop()
    {
        _stopTrackingUseCase.Execute(_sessionState, _activityLogStore, DateTime.Now);
    }
}
