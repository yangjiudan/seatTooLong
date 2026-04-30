using Microsoft.Data.Sqlite;

namespace SeatTooLong.Core.Statistics;

public class SqliteStatisticsRepository : IStatisticsRepository, IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteStatisticsRepository(string connectionString)
    {
        _connection = new SqliteConnection(connectionString);
        _connection.Open();
    }

    public void Initialize()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS SittingSessions (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                StartTime TEXT NOT NULL,
                EndTime TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS RestSessions (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                StartTime TEXT NOT NULL,
                EndTime TEXT NOT NULL
            );";
        cmd.ExecuteNonQuery();
    }

    public void RecordSittingSession(DateTime startTime, DateTime endTime)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "INSERT INTO SittingSessions (StartTime, EndTime) VALUES (@start, @end)";
        cmd.Parameters.AddWithValue("@start", startTime.ToString("o"));
        cmd.Parameters.AddWithValue("@end", endTime.ToString("o"));
        cmd.ExecuteNonQuery();
    }

    public void RecordRestSession(DateTime startTime, DateTime endTime)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "INSERT INTO RestSessions (StartTime, EndTime) VALUES (@start, @end)";
        cmd.Parameters.AddWithValue("@start", startTime.ToString("o"));
        cmd.Parameters.AddWithValue("@end", endTime.ToString("o"));
        cmd.ExecuteNonQuery();
    }

    public void UpsertSittingSession(DateTime startTime, DateTime endTime)
    {
        UpsertSession("SittingSessions", startTime, endTime);
    }

    public void UpsertRestSession(DateTime startTime, DateTime endTime)
    {
        UpsertSession("RestSessions", startTime, endTime);
    }

    private void UpsertSession(string tableName, DateTime startTime, DateTime endTime)
    {
        var existingId = FindSessionId(tableName, startTime);

        using var cmd = _connection.CreateCommand();
        cmd.Parameters.AddWithValue("@end", endTime.ToString("o"));

        if (existingId.HasValue)
        {
            cmd.CommandText = $"UPDATE {tableName} SET EndTime = @end WHERE Id = @id";
            cmd.Parameters.AddWithValue("@id", existingId.Value);
        }
        else
        {
            cmd.CommandText = $"INSERT INTO {tableName} (StartTime, EndTime) VALUES (@start, @end)";
            cmd.Parameters.AddWithValue("@start", startTime.ToString("o"));
        }

        cmd.ExecuteNonQuery();
    }

    private long? FindSessionId(string tableName, DateTime startTime)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = $"SELECT Id FROM {tableName} WHERE StartTime = @start ORDER BY Id LIMIT 1";
        cmd.Parameters.AddWithValue("@start", startTime.ToString("o"));
        var result = cmd.ExecuteScalar();
        return result == null || result == DBNull.Value ? null : Convert.ToInt64(result);
    }

    public IReadOnlyList<SittingSession> GetSittingSessions(DateTime date)
    {
        var sessions = new List<SittingSession>();
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"SELECT Id, StartTime, EndTime FROM SittingSessions 
                           WHERE date(StartTime) = @date ORDER BY StartTime";
        cmd.Parameters.AddWithValue("@date", date.ToString("yyyy-MM-dd"));
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            sessions.Add(new SittingSession
            {
                Id = reader.GetInt64(0),
                StartTime = DateTime.Parse(reader.GetString(1)),
                EndTime = DateTime.Parse(reader.GetString(2))
            });
        }
        return sessions;
    }

    public IReadOnlyList<RestSession> GetRestSessions(DateTime date)
    {
        var sessions = new List<RestSession>();
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"SELECT Id, StartTime, EndTime FROM RestSessions 
                           WHERE date(StartTime) = @date ORDER BY StartTime";
        cmd.Parameters.AddWithValue("@date", date.ToString("yyyy-MM-dd"));
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            sessions.Add(new RestSession
            {
                Id = reader.GetInt64(0),
                StartTime = DateTime.Parse(reader.GetString(1)),
                EndTime = DateTime.Parse(reader.GetString(2))
            });
        }
        return sessions;
    }

    public DailySummary GetDailySummary(DateTime date)
    {
        var sittingSessions = GetSittingSessions(date);
        var restSessions = GetRestSessions(date);

        var totalSitting = TimeSpan.Zero;
        var longestSitting = TimeSpan.Zero;

        foreach (var s in sittingSessions)
        {
            totalSitting += s.Duration;
            if (s.Duration > longestSitting)
                longestSitting = s.Duration;
        }

        return new DailySummary
        {
            Date = date.Date,
            TotalSittingDuration = totalSitting,
            StandUpCount = restSessions.Count,
            LongestContinuousSitting = longestSitting
        };
    }

    public IReadOnlyList<DailySummary> GetDailySummaries(DateTime from, DateTime to)
    {
        var summaries = new List<DailySummary>();
        for (var d = from.Date; d <= to.Date; d = d.AddDays(1))
        {
            summaries.Add(GetDailySummary(d));
        }
        return summaries;
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
