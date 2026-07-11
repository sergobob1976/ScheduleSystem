using System.Data;
using Dapper;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using Schedule.Core.Interfaces;
using Schedule.Core.Models;
using Schedule.Core.DTOs;
using Schedule.Core.Enums;

namespace Schedule.Infrastructure.Repositories;

public class RealLessonRepository : IRealLessonRepository
{
    private readonly string _connectionString;

    private const string BaseJoinSql = """
        SELECT
            rl.*,
            g.*,
            t.*,
            d.*,
            c.*,
            s.*

        FROM `RealLessons` rl

        INNER JOIN `Groups` g
            ON g.Id = rl.GroupId

        INNER JOIN `Teachers` t
            ON t.Id = rl.TeacherId

        INNER JOIN `Disciplines` d
            ON d.Id = rl.DisciplineId

        LEFT JOIN `ClassRooms` c
            ON c.Id = rl.ClassRoomId

        INNER JOIN `Semesters` s
            ON s.Id = rl.SemesterId
        """;

    public RealLessonRepository(
        IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString(
                "DefaultConnection")
            ?? throw new InvalidOperationException(
                "Рядок підключення не знайдено.");
    }

    private IDbConnection CreateConnection()
    {
        return new MySqlConnection(_connectionString);
    }

    public async Task<IEnumerable<RealLesson>> GetAllAsync()
    {
        using var connection = CreateConnection();

        string sql = $"""
            {BaseJoinSql}
            ORDER BY
                rl.LessonDate,
                rl.LessonPosition,
                g.Name;
            """;

        return await QueryAsync(connection, sql);
    }

    public async Task<RealLesson?> GetByIdAsync(int id)
    {
        using var connection = CreateConnection();

        string sql = $"""
            {BaseJoinSql}
            WHERE rl.Id = @Id;
            """;

        var results =
            await QueryAsync(
                connection,
                sql,
                new { Id = id });

        return results.FirstOrDefault();
    }

    public async Task<IEnumerable<RealLesson>>
        GetByGroupIdAsync(int groupId)
    {
        using var connection = CreateConnection();

        string sql = $"""
            {BaseJoinSql}
            WHERE rl.GroupId = @GroupId
            ORDER BY
                rl.LessonDate,
                rl.LessonPosition;
            """;

        return await QueryAsync(
            connection,
            sql,
            new { GroupId = groupId });
    }

    public async Task<IEnumerable<RealLesson>>
        GetByTeacherIdAsync(int teacherId)
    {
        using var connection = CreateConnection();

        string sql = $"""
            {BaseJoinSql}
            WHERE rl.TeacherId = @TeacherId
            ORDER BY
                rl.LessonDate,
                rl.LessonPosition;
            """;

        return await QueryAsync(
            connection,
            sql,
            new { TeacherId = teacherId });
    }

    public async Task<IEnumerable<RealLesson>>
        GetConflictingLessonsAsync(
            RealLesson lesson,
            int? excludedId = null)
    {
        using var connection = CreateConnection();

        string sql = $"""
            {BaseJoinSql}
            WHERE
                rl.SemesterId = @SemesterId
                AND rl.LessonDate = @LessonDate
                AND rl.LessonPosition = @LessonPosition
                AND
                (
                    rl.GroupId = @GroupId
                    OR rl.TeacherId = @TeacherId
                    OR
                    (
                        @ClassRoomId IS NOT NULL
                        AND rl.ClassRoomId = @ClassRoomId
                    )
                )
                AND
                (
                    @ExcludedId IS NULL
                    OR rl.Id <> @ExcludedId
                )
            ORDER BY rl.Id;
            """;

        return await QueryAsync(
            connection,
            sql,
            new
            {
                lesson.SemesterId,
                LessonDate = lesson.LessonDate.Date,
                lesson.LessonPosition,
                lesson.GroupId,
                lesson.TeacherId,
                lesson.ClassRoomId,
                ExcludedId = excludedId
            });
    }

    public async Task<int> CreateAsync(
        RealLesson lesson)
    {
        using var connection = CreateConnection();

        const string sql = """
            INSERT INTO `RealLessons`
            (
                TeachingAssignmentId,
                GroupId,
                TeacherId,
                DisciplineId,
                ClassRoomId,
                SemesterId,
                LessonDate,
                LessonPosition,
                WeekDay,
                WeekProperty,
                LessonType,
                Status,
                ConferenceLink,
                ResourceLink
            )
            VALUES
            (
                @TeachingAssignmentId,
                @GroupId,
                @TeacherId,
                @DisciplineId,
                @ClassRoomId,
                @SemesterId,
                @LessonDate,
                @LessonPosition,
                @WeekDay,
                @WeekProperty,
                @LessonType,
                @Status,
                @ConferenceLink,
                @ResourceLink
            );

            SELECT CAST(LAST_INSERT_ID() AS SIGNED);
            """;

        return await connection.QuerySingleAsync<int>(
            sql,
            lesson);
    }

    public async Task<bool> UpdateAsync(
        RealLesson lesson)
    {
        using var connection = CreateConnection();

        const string sql = """
            UPDATE `RealLessons`
            SET
                TeachingAssignmentId =
                    @TeachingAssignmentId,
                GroupId = @GroupId,
                TeacherId = @TeacherId,
                DisciplineId = @DisciplineId,
                ClassRoomId = @ClassRoomId,
                SemesterId = @SemesterId,
                LessonDate = @LessonDate,
                LessonPosition = @LessonPosition,
                WeekDay = @WeekDay,
                WeekProperty = @WeekProperty,
                LessonType = @LessonType,
                Status = @Status,
                ConferenceLink = @ConferenceLink,
                ResourceLink = @ResourceLink
            WHERE Id = @Id;
            """;

        int rowsAffected =
            await connection.ExecuteAsync(
                sql,
                lesson);

        return rowsAffected > 0;
    }

    public async Task<bool> UpdateStatusAsync(
        int lessonId,
        RealLessonStatus status)
    {
        using var connection = CreateConnection();

        const string sql = """
            UPDATE `RealLessons`
            SET Status = @Status
            WHERE Id = @Id;
            """;

        int rowsAffected =
            await connection.ExecuteAsync(
                sql,
                new
                {
                    Id = lessonId,
                    Status = status
                });

        return rowsAffected > 0;
    }

    public async Task<bool> UpdateLinksAsync(
        int lessonId,
        string? conferenceLink,
        string? resourceLink)
    {
        using var connection = CreateConnection();

        const string sql = """
            UPDATE `RealLessons`
            SET
                ConferenceLink = @ConferenceLink,
                ResourceLink = @ResourceLink
            WHERE Id = @Id;
            """;

        int rowsAffected =
            await connection.ExecuteAsync(
                sql,
                new
                {
                    Id = lessonId,
                    ConferenceLink = conferenceLink,
                    ResourceLink = resourceLink
                });

        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var connection = CreateConnection();

        const string sql = """
            DELETE FROM `RealLessons`
            WHERE Id = @Id;
            """;

        int rowsAffected =
            await connection.ExecuteAsync(
                sql,
                new { Id = id });

        return rowsAffected > 0;
    }

    public async Task<TransferRealLessonWeekResult>
        TransferWeekAsync(
            int semesterId,
            DateTime weekStartDate,
            DateTime weekEndDate,
            WeekProperty weekProperty,
            IReadOnlyCollection<RealLesson> lessons)
    {
        await using var connection =
            new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var transaction =
            await connection.BeginTransactionAsync();

        const string transferSql = """
            INSERT INTO `RealLessonWeekTransfers`
            (
                SemesterId,
                WeekStartDate,
                WeekEndDate,
                WeekProperty
            )
            VALUES
            (
                @SemesterId,
                @WeekStartDate,
                @WeekEndDate,
                @WeekProperty
            );
            """;

        try
        {
            await connection.ExecuteAsync(
                transferSql,
                new
                {
                    SemesterId = semesterId,
                    WeekStartDate = weekStartDate.Date,
                    WeekEndDate = weekEndDate.Date,
                    WeekProperty = weekProperty
                },
                transaction);
        }
        catch (MySqlException ex)
            when (ex.Number == 1062)
        {
            await transaction.RollbackAsync();

            return TransferRealLessonWeekResult
                .AlreadyTransferred;
        }

        const string lessonSql = """
            INSERT INTO `RealLessons`
            (
                TeachingAssignmentId,
                GroupId,
                TeacherId,
                DisciplineId,
                ClassRoomId,
                SemesterId,
                LessonDate,
                LessonPosition,
                WeekDay,
                WeekProperty,
                LessonType,
                Status,
                ConferenceLink,
                ResourceLink
            )
            VALUES
            (
                @TeachingAssignmentId,
                @GroupId,
                @TeacherId,
                @DisciplineId,
                @ClassRoomId,
                @SemesterId,
                @LessonDate,
                @LessonPosition,
                @WeekDay,
                @WeekProperty,
                @LessonType,
                @Status,
                @ConferenceLink,
                @ResourceLink
            );
            """;

        await connection.ExecuteAsync(
            lessonSql,
            lessons,
            transaction);

        await transaction.CommitAsync();

        return TransferRealLessonWeekResult.Created;
    }

    public async Task<
        IEnumerable<TransferredRealLessonWeekItem>>
        GetTransferredWeeksAsync(int semesterId)
    {
        using var connection = CreateConnection();

        const string sql = """
            SELECT
                wt.Id,
                wt.SemesterId,
                wt.WeekStartDate,
                wt.WeekEndDate,
                wt.WeekProperty,
                wt.CreatedAt,
                COUNT(lesson.Id) AS LessonCount

            FROM `RealLessonWeekTransfers` wt

            LEFT JOIN `RealLessons` lesson
                ON lesson.SemesterId = wt.SemesterId
                AND lesson.LessonDate >=
                    wt.WeekStartDate
                AND lesson.LessonDate <=
                    wt.WeekEndDate

            WHERE wt.SemesterId = @SemesterId

            GROUP BY
                wt.Id,
                wt.SemesterId,
                wt.WeekStartDate,
                wt.WeekEndDate,
                wt.WeekProperty,
                wt.CreatedAt

            ORDER BY wt.WeekStartDate;
            """;

        return await connection.QueryAsync<
            TransferredRealLessonWeekItem>(
            sql,
            new { SemesterId = semesterId });
    }

    private static async Task<IEnumerable<RealLesson>>
        QueryAsync(
            IDbConnection connection,
            string sql,
            object? parameters = null)
    {
        return await connection.QueryAsync<
            RealLesson,
            Group,
            Teacher,
            Discipline,
            ClassRoom,
            Semester,
            RealLesson>(
            sql,
            (
                lesson,
                group,
                teacher,
                discipline,
                classRoom,
                semester
            ) =>
            {
                lesson.Group = group;
                lesson.Teacher = teacher;
                lesson.Discipline = discipline;
                lesson.ClassRoom = classRoom;
                lesson.Semester = semester;

                return lesson;
            },
            parameters,
            splitOn: "Id,Id,Id,Id,Id");
    }
}
