using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using Dapper;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using Schedule.Core.Enums;
using Schedule.Core.Interfaces;
using Schedule.Core.Models;

namespace Schedule.Infrastructure.Repositories;

public class TeachingAssignmentRepository
    : ITeachingAssignmentRepository
{
    private readonly string _connectionString;

    private const string SelectSql = """
        SELECT
            ta.Id,
            ta.GroupDisciplineId,
            ta.TeacherId,
            ta.LessonType,
            ta.AssignedHours,

            gd.Id,
            gd.SemesterId,
            gd.GroupId,
            gd.DisciplineId,
            gd.LectureHours,
            gd.PracticalHours,
            gd.LaboratoryHours,
            gd.SeminarHours,
            gd.OtherHours,

            t.Id,
            t.Name,
            t.Position

        FROM `TeachingAssignments` ta

        INNER JOIN `GroupDisciplines` gd
            ON gd.Id = ta.GroupDisciplineId

        INNER JOIN `Teachers` t
            ON t.Id = ta.TeacherId
        """;

    public TeachingAssignmentRepository(
        IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Рядок підключення до бази даних не знайдено.");
    }

    private IDbConnection CreateConnection()
    {
        return new MySqlConnection(_connectionString);
    }

    public async Task<IEnumerable<TeachingAssignment>>
        GetAllAsync()
    {
        using var connection = CreateConnection();

        string sql = $"""
            {SelectSql}
            ORDER BY
                gd.SemesterId,
                gd.GroupId,
                gd.DisciplineId,
                ta.LessonType,
                t.Name;
            """;

        return await QueryAsync(connection, sql);
    }

    public async Task<IEnumerable<TeachingAssignment>>
        GetByGroupDisciplineIdAsync(
            int groupDisciplineId)
    {
        using var connection = CreateConnection();

        string sql = $"""
            {SelectSql}
            WHERE ta.GroupDisciplineId =
                @GroupDisciplineId
            ORDER BY ta.LessonType, t.Name;
            """;

        return await QueryAsync(
            connection,
            sql,
            new
            {
                GroupDisciplineId =
                    groupDisciplineId
            });
    }

    public async Task<IEnumerable<TeachingAssignment>>
        GetByTeacherIdAsync(int teacherId)
    {
        using var connection = CreateConnection();

        string sql = $"""
            {SelectSql}
            WHERE ta.TeacherId = @TeacherId
            ORDER BY
                gd.SemesterId,
                gd.DisciplineId,
                gd.GroupId,
                ta.LessonType;
            """;

        return await QueryAsync(
            connection,
            sql,
            new { TeacherId = teacherId });
    }

    public async Task<TeachingAssignment?> GetByIdAsync(
        int id)
    {
        using var connection = CreateConnection();

        string sql = $"""
            {SelectSql}
            WHERE ta.Id = @Id;
            """;

        var result = await QueryAsync(
            connection,
            sql,
            new { Id = id });

        return result.FirstOrDefault();
    }

    public async Task<TeachingAssignment?>
        GetExistingAsync(
            int groupDisciplineId,
            int teacherId,
            LessonType lessonType)
    {
        using var connection = CreateConnection();

        string sql = $"""
            {SelectSql}
            WHERE
                ta.GroupDisciplineId =
                    @GroupDisciplineId
                AND ta.TeacherId = @TeacherId
                AND ta.LessonType = @LessonType;
            """;

        var result = await QueryAsync(
            connection,
            sql,
            new
            {
                GroupDisciplineId =
                    groupDisciplineId,
                TeacherId = teacherId,
                LessonType = lessonType
            });

        return result.FirstOrDefault();
    }

    public async Task<int>
        GetAssignedHoursForGroupDisciplineTypeAsync(
            int groupDisciplineId,
            LessonType lessonType,
            int? excludedId = null)
    {
        using var connection = CreateConnection();

        string sql = """
            SELECT COALESCE(SUM(AssignedHours), 0)
            FROM `TeachingAssignments`
            WHERE
                GroupDisciplineId =
                    @GroupDisciplineId
                AND LessonType = @LessonType
                AND
                (
                    @ExcludedId IS NULL
                    OR Id <> @ExcludedId
                );
            """;

        return await connection.ExecuteScalarAsync<int>(
            sql,
            new
            {
                GroupDisciplineId =
                    groupDisciplineId,
                LessonType = lessonType,
                ExcludedId = excludedId
            });
    }

    public async Task<int>
        GetAssignedHoursForTeacherDisciplineAsync(
            int teacherId,
            int semesterId,
            int disciplineId,
            int? excludedId = null)
    {
        using var connection = CreateConnection();

        string sql = """
            SELECT COALESCE(SUM(ta.AssignedHours), 0)

            FROM `TeachingAssignments` ta

            INNER JOIN `GroupDisciplines` gd
                ON gd.Id = ta.GroupDisciplineId

            WHERE
                ta.TeacherId = @TeacherId
                AND gd.SemesterId = @SemesterId
                AND gd.DisciplineId = @DisciplineId
                AND
                (
                    @ExcludedId IS NULL
                    OR ta.Id <> @ExcludedId
                );
            """;

        return await connection.ExecuteScalarAsync<int>(
            sql,
            new
            {
                TeacherId = teacherId,
                SemesterId = semesterId,
                DisciplineId = disciplineId,
                ExcludedId = excludedId
            });
    }

    public async Task<int> CreateAsync(
        TeachingAssignment teachingAssignment)
    {
        using var connection = CreateConnection();

        string sql = """
            INSERT INTO `TeachingAssignments`
            (
                GroupDisciplineId,
                TeacherId,
                LessonType,
                AssignedHours
            )
            VALUES
            (
                @GroupDisciplineId,
                @TeacherId,
                @LessonType,
                @AssignedHours
            );

            SELECT LAST_INSERT_ID();
            """;

        return await connection.ExecuteScalarAsync<int>(
            sql,
            teachingAssignment);
    }

    public async Task<bool> UpdateAsync(
        TeachingAssignment teachingAssignment)
    {
        using var connection = CreateConnection();

        string sql = """
            UPDATE `TeachingAssignments`
            SET
                GroupDisciplineId =
                    @GroupDisciplineId,
                TeacherId = @TeacherId,
                LessonType = @LessonType,
                AssignedHours = @AssignedHours
            WHERE Id = @Id;
            """;

        int rowsAffected =
            await connection.ExecuteAsync(
                sql,
                teachingAssignment);

        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var connection = CreateConnection();

        string sql = """
            DELETE FROM `TeachingAssignments`
            WHERE Id = @Id;
            """;

        int rowsAffected =
            await connection.ExecuteAsync(
                sql,
                new { Id = id });

        return rowsAffected > 0;
    }

    private static async Task<
        IEnumerable<TeachingAssignment>>
        QueryAsync(
            IDbConnection connection,
            string sql,
            object? parameters = null)
    {
        return await connection.QueryAsync<
            TeachingAssignment,
            GroupDiscipline,
            Teacher,
            TeachingAssignment>(
            sql,
            (
                teachingAssignment,
                groupDiscipline,
                teacher
            ) =>
            {
                teachingAssignment.GroupDiscipline =
                    groupDiscipline;

                teachingAssignment.Teacher = teacher;

                return teachingAssignment;
            },
            parameters,
            splitOn: "Id,Id");
    }
}