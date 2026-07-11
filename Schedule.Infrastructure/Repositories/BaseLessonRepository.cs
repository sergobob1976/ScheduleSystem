using System.Data;
using Dapper;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using Schedule.Core.Enums;
using Schedule.Core.Interfaces;
using Schedule.Core.Models;

namespace Schedule.Infrastructure.Repositories;

public class BaseLessonRepository : IBaseLessonRepository
{
    private readonly string _connectionString;

    private const string BaseJoinSql = """
        SELECT
            bl.*,
            g.*,
            t.*,
            d.*,
            c.*,
            s.*

        FROM `BaseLessons` bl

        INNER JOIN `Groups` g
            ON g.Id = bl.GroupId

        INNER JOIN `Teachers` t
            ON t.Id = bl.TeacherId

        INNER JOIN `Disciplines` d
            ON d.Id = bl.DisciplineId

        LEFT JOIN `ClassRooms` c
            ON c.Id = bl.ClassRoomId

        INNER JOIN `Semesters` s
            ON s.Id = bl.SemesterId
        """;

    public BaseLessonRepository(
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

    public async Task<IEnumerable<BaseLesson>> GetAllAsync()
    {
        using var connection = CreateConnection();

        string sql = $"""
            {BaseJoinSql}
            ORDER BY
                s.StartDate DESC,
                g.Name,
                bl.WeekDay,
                bl.LessonPosition;
            """;

        return await QueryAsync(connection, sql);
    }

    public async Task<BaseLesson?> GetByIdAsync(int id)
    {
        using var connection = CreateConnection();

        string sql = $"""
            {BaseJoinSql}
            WHERE bl.Id = @Id;
            """;

        var results =
            await QueryAsync(
                connection,
                sql,
                new { Id = id });

        return results.FirstOrDefault();
    }

    public async Task<IEnumerable<BaseLesson>>
        GetByGroupIdAsync(int groupId)
    {
        using var connection = CreateConnection();

        string sql = $"""
            {BaseJoinSql}
            WHERE bl.GroupId = @GroupId
            ORDER BY
                s.StartDate DESC,
                bl.WeekDay,
                bl.LessonPosition;
            """;

        return await QueryAsync(
            connection,
            sql,
            new { GroupId = groupId });
    }

    public async Task<IEnumerable<BaseLesson>>
        GetBySemesterIdAsync(int semesterId)
    {
        using var connection = CreateConnection();

        string sql = $"""
            {BaseJoinSql}
            WHERE bl.SemesterId = @SemesterId
            ORDER BY
                g.Name,
                bl.WeekDay,
                bl.LessonPosition;
            """;

        return await QueryAsync(
            connection,
            sql,
            new { SemesterId = semesterId });
    }

    public async Task<IEnumerable<BaseLesson>>
        GetConflictingLessonsAsync(
            BaseLesson lesson,
            int? excludedId = null)
    {
        using var connection = CreateConnection();

        string sql = $"""
            {BaseJoinSql}
            WHERE
                bl.SemesterId = @SemesterId
                AND bl.WeekDay = @WeekDay
                AND bl.LessonPosition = @LessonPosition

                AND
                (
                    bl.WeekProperty = @EveryWeek
                    OR @WeekProperty = @EveryWeek
                    OR bl.WeekProperty = @WeekProperty
                )

                AND
                (
                    bl.GroupId = @GroupId
                    OR bl.TeacherId = @TeacherId
                    OR
                    (
                        @ClassRoomId IS NOT NULL
                        AND bl.ClassRoomId = @ClassRoomId
                    )
                )

                AND
                (
                    @ExcludedId IS NULL
                    OR bl.Id <> @ExcludedId
                )

            ORDER BY bl.Id;
            """;

        return await QueryAsync(
            connection,
            sql,
            new
            {
                lesson.SemesterId,
                lesson.WeekDay,
                lesson.LessonPosition,
                lesson.WeekProperty,
                lesson.GroupId,
                lesson.TeacherId,
                lesson.ClassRoomId,
                EveryWeek =
                    WeekProperty.EveryWeek,
                ExcludedId = excludedId
            });
    }

    public async Task<int> CreateAsync(
        BaseLesson lesson)
    {
        using var connection = CreateConnection();

        const string sql = """
            INSERT INTO `BaseLessons`
            (
                TeachingAssignmentId,
                GroupId,
                TeacherId,
                DisciplineId,
                ClassRoomId,
                SemesterId,
                LessonPosition,
                WeekDay,
                WeekProperty,
                LessonType
            )
            VALUES
            (
                @TeachingAssignmentId,
                @GroupId,
                @TeacherId,
                @DisciplineId,
                @ClassRoomId,
                @SemesterId,
                @LessonPosition,
                @WeekDay,
                @WeekProperty,
                @LessonType
            );

            SELECT LAST_INSERT_ID();
            """;

        return await connection.ExecuteScalarAsync<int>(
            sql,
            lesson);
    }

    public async Task<bool> UpdateAsync(
        BaseLesson lesson)
    {
        using var connection = CreateConnection();

        const string sql = """
            UPDATE `BaseLessons`
            SET
                TeachingAssignmentId =
                    @TeachingAssignmentId,
                GroupId = @GroupId,
                TeacherId = @TeacherId,
                DisciplineId = @DisciplineId,
                ClassRoomId = @ClassRoomId,
                SemesterId = @SemesterId,
                LessonPosition = @LessonPosition,
                WeekDay = @WeekDay,
                WeekProperty = @WeekProperty,
                LessonType = @LessonType
            WHERE Id = @Id;
            """;

        int rowsAffected =
            await connection.ExecuteAsync(
                sql,
                lesson);

        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var connection = CreateConnection();

        const string sql = """
            DELETE FROM `BaseLessons`
            WHERE Id = @Id;
            """;

        int rowsAffected =
            await connection.ExecuteAsync(
                sql,
                new { Id = id });

        return rowsAffected > 0;
    }

    private static async Task<IEnumerable<BaseLesson>>
        QueryAsync(
            IDbConnection connection,
            string sql,
            object? parameters = null)
    {
        return await connection.QueryAsync<
            BaseLesson,
            Group,
            Teacher,
            Discipline,
            ClassRoom,
            Semester,
            BaseLesson>(
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
