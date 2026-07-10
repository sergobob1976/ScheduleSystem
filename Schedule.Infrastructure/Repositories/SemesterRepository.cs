using System.Data;
using Dapper;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using Schedule.Core.Interfaces;
using Schedule.Core.Models;

namespace Schedule.Infrastructure.Repositories;

public class SemesterRepository : ISemesterRepository
{
    private readonly string _connectionString;

    public SemesterRepository(
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

    public async Task<IEnumerable<Semester>> GetAllAsync()
    {
        using var connection = CreateConnection();

        string sql = """
            SELECT
                Id,
                Name,
                StartDate,
                EndDate
            FROM `Semesters`
            ORDER BY StartDate DESC, Name;
            """;

        return await connection.QueryAsync<Semester>(sql);
    }

    public async Task<Semester?> GetByIdAsync(int id)
    {
        using var connection = CreateConnection();

        string sql = """
            SELECT
                Id,
                Name,
                StartDate,
                EndDate
            FROM `Semesters`
            WHERE Id = @Id;
            """;

        return await connection
            .QueryFirstOrDefaultAsync<Semester>(
                sql,
                new { Id = id });
    }

    public async Task<int> CreateAsync(
        Semester semester)
    {
        using var connection = CreateConnection();

        string sql = """
            INSERT INTO `Semesters`
            (
                Name,
                StartDate,
                EndDate
            )
            VALUES
            (
                @Name,
                @StartDate,
                @EndDate
            );

            SELECT LAST_INSERT_ID();
            """;

        return await connection.ExecuteScalarAsync<int>(
            sql,
            semester);
    }

    public async Task<bool> UpdateAsync(
        Semester semester)
    {
        using var connection = CreateConnection();

        string sql = """
            UPDATE `Semesters`
            SET
                Name = @Name,
                StartDate = @StartDate,
                EndDate = @EndDate
            WHERE Id = @Id;
            """;

        int rowsAffected =
            await connection.ExecuteAsync(
                sql,
                semester);

        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var connection = CreateConnection();

        string sql = """
            DELETE FROM `Semesters`
            WHERE Id = @Id;
            """;

        int rowsAffected =
            await connection.ExecuteAsync(
                sql,
                new { Id = id });

        return rowsAffected > 0;
    }
}