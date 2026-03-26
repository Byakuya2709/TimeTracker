using TimeTracker.Application.Abstractions;
using TimeTracker.Application.Models;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.Services;

public sealed class TrackingSessionService : ITrackingSessionService
{
    private readonly ITrackingSessionRepository _trackingSessionRepository;

    public TrackingSessionService(ITrackingSessionRepository trackingSessionRepository)
    {
        _trackingSessionRepository = trackingSessionRepository;
    }

    public IReadOnlyList<TrackingSessionModel> GetTrackingSessionsByWeek(DateOnly dateInWeek)
    {
        IReadOnlyList<TrackingSession> sessions = _trackingSessionRepository.GetTrackingSessionsByWeeks(dateInWeek);

        return sessions
            .Select(MapSession)
            .ToList();
    }

    public bool DeleteTrackingSession(Guid sessionId)
    {
        return _trackingSessionRepository.DeleteTrackingSession(sessionId);
    }

    private static TrackingSessionModel MapSession(TrackingSession session)
    {
        IReadOnlyList<TrackingSessionAppUsageModel> appUsages = session.AppUsages
            .OrderByDescending(item => item.Duration)
            .ThenBy(item => item.AppName, StringComparer.OrdinalIgnoreCase)
            .Select(item => new TrackingSessionAppUsageModel
            {
                AppName = item.AppName,
                Duration = item.Duration
            })
            .ToList();

        return new TrackingSessionModel
        {
            Id = session.Id,
            SessionDate = session.SessionDate,
            StartedAt = session.StartedAt,
            EndedAt = session.EndedAt,
            TotalDuration = session.TotalDuration,
            IdleDuration = session.IdleDuration,
            AppUsages = appUsages
        };
    }
}
