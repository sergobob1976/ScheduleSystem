using System.Data;
using Dapper;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using Schedule.Core.Interfaces;
using Schedule.Core.Models;

namespace Schedule.Infrastructure.Repositories;

public class DisciplineRepository : IDisciplineRepository
{
    private readonly string _connectionString;

    private const string SelectSql = """
        SELECT
            d.Id,
            d.SpecialtyId,
            d.Name,
            d.TotalHours,
            d.LectureHours,
            d.PracticalHours,
            d.LaboratoryHours,
            d.SeminarHours,
            d.OtherHours,

            s.Id,
            s.Code,
            s.Name

        FROM `Disciplines` d

        INNER JOIN `Specialties` s
            ON s.Id = d.SpecialtyId
        """;

    public DisciplineRepository(
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

    public async Task<IEnumerable<Discipline>> GetAllAsync()
    {
        using var connection = CreateConnection();

        string sql = $"""
            {SelectSql}
            ORDER BY
                s.Code,
                s.Name,
                d.Name;
            """;

        return await QueryAsync(connection, sql);
    }

    public async Task<Discipline?> GetByIdAsync(int id)
    {
        using var connection = CreateConnection();

        string sql = $"""
            {SelectSql}
            WHERE d.Id = @Id;
            """;

        var result =
            await QueryAsync(
                connection,
                sql,
                new { Id = id });

        return result.FirstOrDefault();
    }

    public async Task<int> CreateAsync(
        Discipline discipline)
    {
        using var connection = CreateConnection();

        const string sql = """
            INSERT INTO `Disciplines`
            (
                SpecialtyId,
                Name,
                TotalHours,
                LectureHours,
                PracticalHours,
                LaboratoryHours,
                SeminarHours,
                OtherHours
            )
            VALUES
            (
                @SpecialtyId,
                @Name,
                NULL,
                NULL,
                NULL,
                NULL,
                NULL,
                NULL
            );

            SELECT LAST_INSERT_ID();
            """;

        return await connection.ExecuteScalarAsync<int>(
            sql,
            discipline);
    }

    public async Task<bool> UpdateAsync(
        Discipline discipline)
    {
        using var connection = CreateConnection();

        const string sql = """
            UPDATE `Disciplines`
            SET
                SpecialtyId = @SpecialtyId,
                Name = @Name,
                TotalHours = NULL,
                LectureHours = NULL,
                PracticalHours = NULL,
                LaboratoryHours = NULL,
                SeminarHours = NULL,
                OtherHours = NULL
            WHERE Id = @Id;
            """;

        int rowsAffected =
            await connection.ExecuteAsync(
                sql,
                discipline);

        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var connection = CreateConnection();

        const string sql = """
            DELETE FROM `Disciplines`
            WHERE Id = @Id;
            """;

        int rowsAffected =
            await connection.ExecuteAsync(
                sql,
                new { Id = id });

        return rowsAffected > 0;
    }

    private static async Task<IEnumerable<Discipline>>
        QueryAsync(
            IDbConnection connection,
            string sql,
            object? parameters = null)
    {
        return await connection.QueryAsync<
            Discipline,
            Specialty,
            Discipline>(
            sql,
            (discipline, specialty) =>
            {
                discipline.Specialty = specialty;

                return discipline;
            },
            parameters,
            splitOn: "Id");
    }
}