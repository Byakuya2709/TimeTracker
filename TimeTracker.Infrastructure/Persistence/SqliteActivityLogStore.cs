using Microsoft.Data.Sqlite;
using TimeTracker.Application.Abstractions;
using TimeTracker.Domain.Entities;

namespace TimeTracker.Infrastructure.Persistence;

public class SqliteActivityLogStore : IActivityLogStore
{
    private readonly string _connectionString;

    public SqliteActivityLogStore(string databasePath)
    {
        _connectionString = $"Data Source={databasePath}";
        EnsureDatabase();
    }

    public void Add(ActivityLog activityLog)
    {
        using SqliteConnection connection = new(_connectionString);
        connection.Open();

        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO ActivityLogs (Id, AppName, StartTime, EndTime)
            VALUES ($id, $appName, $startTime, $endTime);
            """;

        command.Parameters.AddWithValue("$id", activityLog.Id.ToString());
        command.Parameters.AddWithValue("$appName", activityLog.AppName);
        command.Parameters.AddWithValue("$startTime", activityLog.StartTime.ToString("O"));
        command.Parameters.AddWithValue("$endTime", activityLog.EndTime?.ToString("O") ?? (object)DBNull.Value);

        command.ExecuteNonQuery();
    }

    public void Update(ActivityLog activityLog)
    {
        using SqliteConnection connection = new(_connectionString);
        connection.Open();

        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            UPDATE ActivityLogs
            SET AppName = $appName,
                StartTime = $startTime,
                EndTime = $endTime
            WHERE Id = $id;
            """;

        command.Parameters.AddWithValue("$id", activityLog.Id.ToString());
        command.Parameters.AddWithValue("$appName", activityLog.AppName);
        command.Parameters.AddWithValue("$startTime", activityLog.StartTime.ToString("O"));
        command.Parameters.AddWithValue("$endTime", activityLog.EndTime?.ToString("O") ?? (object)DBNull.Value);

        command.ExecuteNonQuery();
    }

    public IReadOnlyList<ActivityLog> GetAll()
    {
        List<ActivityLog> logs = [];

        using SqliteConnection connection = new(_connectionString);
        connection.Open();

        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT Id, AppName, StartTime, EndTime
            FROM ActivityLogs
            ORDER BY StartTime DESC;
            """;

        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            string id = reader.GetString(0);
            string appName = reader.GetString(1);
            string startTime = reader.GetString(2);
            string? endTime = reader.IsDBNull(3) ? null : reader.GetString(3);

            logs.Add(new ActivityLog
            {
                Id = Guid.Parse(id),
                AppName = appName,
                StartTime = DateTime.Parse(startTime),
                EndTime = string.IsNullOrWhiteSpace(endTime) ? null : DateTime.Parse(endTime)
            });
        }

        return logs;
    }

    public ActivityLog GetActivityLogById(Guid id)
    {
        using SqliteConnection connection = new(_connectionString);
        connection.Open();

        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT Id, AppName, StartTime, EndTime
            FROM ActivityLogs
            WHERE Id = $id;
            """;

        command.Parameters.AddWithValue("$id", id.ToString());

        using SqliteDataReader reader = command.ExecuteReader();
        if (!reader.Read())
        {
            throw new KeyNotFoundException($"ActivityLog with ID {id} not found.");
        }

        string appName = reader.GetString(1);
        string startTime = reader.GetString(2);
        string? endTime = reader.IsDBNull(3) ? null : reader.GetString(3);

        return new ActivityLog
        {
            Id = id,
            AppName = appName,
            StartTime = DateTime.Parse(startTime),
            EndTime = string.IsNullOrWhiteSpace(endTime) ? null : DateTime.Parse(endTime)
        };
    }

    private void EnsureDatabase()
    {
        using SqliteConnection connection = new(_connectionString);
        connection.Open();

        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS ActivityLogs (
                Id TEXT PRIMARY KEY,
                AppName TEXT NOT NULL,
                StartTime TEXT NOT NULL,
                EndTime TEXT NULL
            );
            """;

        command.ExecuteNonQuery();
    }
}
