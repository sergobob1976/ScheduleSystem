using System.Text.Json;
using Microsoft.Data.Sqlite;
using Schedule.Core.DTOs;

namespace Schedule.Maui.Services;

public class ScheduleCacheService
{
    private const string OptionsKey = "mobile-schedule-options";
    private readonly string _connectionString;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public ScheduleCacheService()
    {
        var databasePath = Path.Combine(FileSystem.AppDataDirectory, "schedule_cache.db");
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath
        }.ToString();

        InitializeDatabase();
    }

    public async Task SaveOptionsAsync(MobileScheduleOptionsResponse options)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO CachedOptions (CacheKey, Json, UpdatedAtUtc)
            VALUES ($cacheKey, $json, $updatedAtUtc)
            ON CONFLICT(CacheKey) DO UPDATE SET
                Json = excluded.Json,
                UpdatedAtUtc = excluded.UpdatedAtUtc;
            """;
        command.Parameters.AddWithValue("$cacheKey", OptionsKey);
        command.Parameters.AddWithValue("$json", JsonSerializer.Serialize(options, _jsonOptions));
        command.Parameters.AddWithValue("$updatedAtUtc", DateTime.UtcNow.ToString("O"));
        await command.ExecuteNonQueryAsync();
    }

    public async Task<CachedValue<MobileScheduleOptionsResponse>?> GetOptionsAsync()
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Json, UpdatedAtUtc
            FROM CachedOptions
            WHERE CacheKey = $cacheKey;
            """;
        command.Parameters.AddWithValue("$cacheKey", OptionsKey);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        var value = JsonSerializer.Deserialize<MobileScheduleOptionsResponse>(reader.GetString(0), _jsonOptions);
        return value is null
            ? null
            : new CachedValue<MobileScheduleOptionsResponse>(value, DateTime.Parse(reader.GetString(1)).ToLocalTime());
    }

    public async Task SaveDayAsync(
        bool forTeacher,
        int filterId,
        DateTime date,
        IReadOnlyCollection<MobileScheduleLessonResponse> lessons)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO CachedScheduleDays
                (ViewerMode, FilterId, LessonDate, Json, UpdatedAtUtc)
            VALUES
                ($viewerMode, $filterId, $lessonDate, $json, $updatedAtUtc)
            ON CONFLICT(ViewerMode, FilterId, LessonDate) DO UPDATE SET
                Json = excluded.Json,
                UpdatedAtUtc = excluded.UpdatedAtUtc;
            """;
        command.Parameters.AddWithValue("$viewerMode", forTeacher ? "Teacher" : "Student");
        command.Parameters.AddWithValue("$filterId", filterId);
        command.Parameters.AddWithValue("$lessonDate", date.Date.ToString("yyyy-MM-dd"));
        command.Parameters.AddWithValue("$json", JsonSerializer.Serialize(lessons, _jsonOptions));
        command.Parameters.AddWithValue("$updatedAtUtc", DateTime.UtcNow.ToString("O"));
        await command.ExecuteNonQueryAsync();
    }

    public async Task<CachedValue<List<MobileScheduleLessonResponse>>?> GetDayAsync(
        bool forTeacher,
        int filterId,
        DateTime date)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Json, UpdatedAtUtc
            FROM CachedScheduleDays
            WHERE ViewerMode = $viewerMode
                AND FilterId = $filterId
                AND LessonDate = $lessonDate;
            """;
        command.Parameters.AddWithValue("$viewerMode", forTeacher ? "Teacher" : "Student");
        command.Parameters.AddWithValue("$filterId", filterId);
        command.Parameters.AddWithValue("$lessonDate", date.Date.ToString("yyyy-MM-dd"));

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        var value = JsonSerializer.Deserialize<List<MobileScheduleLessonResponse>>(reader.GetString(0), _jsonOptions);
        return value is null
            ? null
            : new CachedValue<List<MobileScheduleLessonResponse>>(value, DateTime.Parse(reader.GetString(1)).ToLocalTime());
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS CachedOptions
            (
                CacheKey TEXT PRIMARY KEY,
                Json TEXT NOT NULL,
                UpdatedAtUtc TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS CachedScheduleDays
            (
                ViewerMode TEXT NOT NULL,
                FilterId INTEGER NOT NULL,
                LessonDate TEXT NOT NULL,
                Json TEXT NOT NULL,
                UpdatedAtUtc TEXT NOT NULL,
                PRIMARY KEY (ViewerMode, FilterId, LessonDate)
            );
            """;
        command.ExecuteNonQuery();
    }
}

public record CachedValue<T>(T Value, DateTime UpdatedAt);
