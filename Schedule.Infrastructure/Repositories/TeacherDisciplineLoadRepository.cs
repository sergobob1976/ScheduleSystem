using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using Dapper;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using Schedule.Core.Interfaces;
using Schedule.Core.Models;

namespace Schedule.Infrastructure.Repositories;

public class TeacherDisciplineLoadRepository
    : ITeacherDisciplineLoadRepository
{
    private readonly string _connectionString;

    private const string SelectSql = """
        SELECT
            tdl.Id,
            tdl.TeacherSemesterLoadId,
            tdl.DisciplineId,
            tdl.PlannedHours,

            tsl.Id,
            tsl.TeacherId,
            tsl.SemesterId,
            tsl.PlannedHours,

            d.Id,
            d.SpecialtyId,
            d.Name,
            d.TotalHours,
            d.LectureHours,
            d.PracticalHours,
            d.LaboratoryHours,
            d.SeminarHours,
            d.OtherHours

        FROM `TeacherDisciplineLoads` tdl

        INNER JOIN `TeacherSemesterLoads` tsl
            ON tsl.Id = tdl.TeacherSemesterLoadId

        INNER JOIN `Disciplines` d
            ON d.Id = tdl.DisciplineId
        """;

    public TeacherDisciplineLoadRepository(
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

    public async Task<IEnumerable<TeacherDisciplineLoad>>
        GetAllAsync()
    {
        using var connection = CreateConnection();

        string sql = $"""
            {SelectSql}
            ORDER BY
                tsl.SemesterId,
                tsl.TeacherId,
                d.Name;
            """;

        return await QueryAsync(connection, sql);
    }

    public async Task<IEnumerable<TeacherDisciplineLoad>>
        GetByTeacherSemesterLoadIdAsync(
            int teacherSemesterLoadId)
    {
        using var connection = CreateConnection();

        string sql = $"""
            {SelectSql}
            WHERE tdl.TeacherSemesterLoadId =
                @TeacherSemesterLoadId
            ORDER BY d.Name;
            """;

        return await QueryAsync(
            connection,
            sql,
            new
            {
                TeacherSemesterLoadId =
                    teacherSemesterLoadId
            });
    }

    public async Task<TeacherDisciplineLoad?> GetByIdAsync(
        int id)
    {
        using var connection = CreateConnection();

        string sql = $"""
            {SelectSql}
            WHERE tdl.Id = @Id;
            """;

        var result = await QueryAsync(
            connection,
            sql,
            new { Id = id });

        return result.FirstOrDefault();
    }

    public async Task<TeacherDisciplineLoad?>
        GetByLoadAndDisciplineAsync(
            int teacherSemesterLoadId,
            int disciplineId)
    {
        using var connection = CreateConnection();

        string sql = $"""
            {SelectSql}
            WHERE
                tdl.TeacherSemesterLoadId =
                    @TeacherSemesterLoadId
                AND tdl.DisciplineId = @DisciplineId;
            """;

        var result = await QueryAsync(
            connection,
            sql,
            new
            {
                TeacherSemesterLoadId =
                    teacherSemesterLoadId,
                DisciplineId = disciplineId
            });

        return result.FirstOrDefault();
    }

    public async Task<int> GetTotalPlannedHoursAsync(
        int teacherSemesterLoadId,
        int? excludedId = null)
    {
        using var connection = CreateConnection();

        string sql = """
            SELECT COALESCE(SUM(PlannedHours), 0)
            FROM `TeacherDisciplineLoads`
            WHERE
                TeacherSemesterLoadId =
                    @TeacherSemesterLoadId
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
                TeacherSemesterLoadId =
                    teacherSemesterLoadId,
                ExcludedId = excludedId
            });
    }

    public async Task<int> CreateAsync(
        TeacherDisciplineLoad teacherDisciplineLoad)
    {
        using var connection = CreateConnection();

        string sql = """
            INSERT INTO `TeacherDisciplineLoads`
            (
                TeacherSemesterLoadId,
                DisciplineId,
                PlannedHours
            )
            VALUES
            (
                @TeacherSemesterLoadId,
                @DisciplineId,
                @PlannedHours
            );

            SELECT LAST_INSERT_ID();
            """;

        return await connection.ExecuteScalarAsync<int>(
            sql,
            teacherDisciplineLoad);
    }

    public async Task<bool> UpdateAsync(
        TeacherDisciplineLoad teacherDisciplineLoad)
    {
        using var connection = CreateConnection();

        string sql = """
            UPDATE `TeacherDisciplineLoads`
            SET
                TeacherSemesterLoadId =
                    @TeacherSemesterLoadId,
                DisciplineId = @DisciplineId,
                PlannedHours = @PlannedHours
            WHERE Id = @Id;
            """;

        int rowsAffected =
            await connection.ExecuteAsync(
                sql,
                teacherDisciplineLoad);

        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var connection = CreateConnection();

        string sql = """
            DELETE FROM `TeacherDisciplineLoads`
            WHERE Id = @Id;
            """;

        int rowsAffected =
            await connection.ExecuteAsync(
                sql,
                new { Id = id });

        return rowsAffected > 0;
    }

    private static async Task<
        IEnumerable<TeacherDisciplineLoad>>
        QueryAsync(
            IDbConnection connection,
            string sql,
            object? parameters = null)
    {
        return await connection.QueryAsync<
            TeacherDisciplineLoad,
            TeacherSemesterLoad,
            Discipline,
            TeacherDisciplineLoad>(
            sql,
            (
                teacherDisciplineLoad,
                teacherSemesterLoad,
                discipline
            ) =>
            {
                teacherDisciplineLoad
                    .TeacherSemesterLoad =
                    teacherSemesterLoad;

                teacherDisciplineLoad.Discipline =
                    discipline;

                return teacherDisciplineLoad;
            },
            parameters,
            splitOn: "Id,Id");
    }
}