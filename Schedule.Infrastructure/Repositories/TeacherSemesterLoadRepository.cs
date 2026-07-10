using System.Data;
using Dapper;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using Schedule.Core.Interfaces;
using Schedule.Core.Models;

namespace Schedule.Infrastructure.Repositories;

public class TeacherSemesterLoadRepository : ITeacherSemesterLoadRepository
{
    private readonly string _connectionString;

    private const string SelectSql = """
        SELECT
            tsl.Id,
            tsl.TeacherId,
            tsl.SemesterId,
            tsl.PlannedHours,

            t.Id,
            t.Name,
            t.Position,

            s.Id,
            s.Name,
            s.StartDate,
            s.EndDate

        FROM `TeacherSemesterLoads` tsl

        INNER JOIN `Teachers` t
            ON t.Id = tsl.TeacherId

        INNER JOIN `Semesters` s
            ON s.Id = tsl.SemesterId
        """;

    public TeacherSemesterLoadRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Рядок підключення до бази даних не знайдено.");
    }

    private IDbConnection CreateConnection()
    {
        return new MySqlConnection(_connectionString);
    }

    public async Task<IEnumerable<TeacherSemesterLoad>> GetAllAsync()
    {
        using var connection = CreateConnection();

        string sql = $"""
            {SelectSql}
            ORDER BY
                s.StartDate DESC,
                t.Name;
            """;

        return await QueryAsync(connection, sql);
    }

    public async Task<IEnumerable<TeacherSemesterLoad>> GetBySemesterIdAsync(
        int semesterId)
    {
        using var connection = CreateConnection();

        string sql = $"""
            {SelectSql}
            WHERE tsl.SemesterId = @SemesterId
            ORDER BY t.Name;
            """;

        return await QueryAsync(
            connection,
            sql,
            new { SemesterId = semesterId });
    }

    public async Task<IEnumerable<TeacherSemesterLoad>> GetByTeacherIdAsync(
        int teacherId)
    {
        using var connection = CreateConnection();

        string sql = $"""
            {SelectSql}
            WHERE tsl.TeacherId = @TeacherId
            ORDER BY s.StartDate DESC;
            """;

        return await QueryAsync(
            connection,
            sql,
            new { TeacherId = teacherId });
    }

    public async Task<TeacherSemesterLoad?> GetByIdAsync(int id)
    {
        using var connection = CreateConnection();

        string sql = $"""
            {SelectSql}
            WHERE tsl.Id = @Id;
            """;

        var result = await QueryAsync(
            connection,
            sql,
            new { Id = id });

        return result.FirstOrDefault();
    }

    public async Task<TeacherSemesterLoad?> GetByTeacherAndSemesterAsync(
        int teacherId,
        int semesterId)
    {
        using var connection = CreateConnection();

        string sql = $"""
            {SelectSql}
            WHERE
                tsl.TeacherId = @TeacherId
                AND tsl.SemesterId = @SemesterId;
            """;

        var result = await QueryAsync(
            connection,
            sql,
            new
            {
                TeacherId = teacherId,
                SemesterId = semesterId
            });

        return result.FirstOrDefault();
    }

    public async Task<int> CreateAsync(
        TeacherSemesterLoad teacherSemesterLoad)
    {
        using var connection = CreateConnection();

        string sql = """
            INSERT INTO `TeacherSemesterLoads`
            (
                TeacherId,
                SemesterId,
                PlannedHours
            )
            VALUES
            (
                @TeacherId,
                @SemesterId,
                @PlannedHours
            );

            SELECT LAST_INSERT_ID();
            """;

        return await connection.ExecuteScalarAsync<int>(
            sql,
            teacherSemesterLoad);
    }

    public async Task<bool> UpdateAsync(
        TeacherSemesterLoad teacherSemesterLoad)
    {
        using var connection = CreateConnection();

        string sql = """
            UPDATE `TeacherSemesterLoads`
            SET
                TeacherId = @TeacherId,
                SemesterId = @SemesterId,
                PlannedHours = @PlannedHours
            WHERE Id = @Id;
            """;

        int rowsAffected = await connection.ExecuteAsync(
            sql,
            teacherSemesterLoad);

        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var connection = CreateConnection();

        string sql = """
            DELETE FROM `TeacherSemesterLoads`
            WHERE Id = @Id;
            """;

        int rowsAffected = await connection.ExecuteAsync(
            sql,
            new { Id = id });

        return rowsAffected > 0;
    }

    private static async Task<IEnumerable<TeacherSemesterLoad>> QueryAsync(
        IDbConnection connection,
        string sql,
        object? parameters = null)
    {
        return await connection.QueryAsync<
            TeacherSemesterLoad,
            Teacher,
            Semester,
            TeacherSemesterLoad>(
            sql,
            (teacherSemesterLoad, teacher, semester) =>
            {
                teacherSemesterLoad.Teacher = teacher;
                teacherSemesterLoad.Semester = semester;

                return teacherSemesterLoad;
            },
            parameters,
            splitOn: "Id,Id");
    }
}