using System.Data;
using Dapper;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using Schedule.Core.Interfaces;
using Schedule.Core.Models;

namespace Schedule.Infrastructure.Repositories;

public class GroupRepository : IGroupRepository
{
    private readonly string _connectionString;

    public GroupRepository(
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

    public async Task<IEnumerable<Group>> GetAllAsync()
    {
        using var connection = CreateConnection();

        const string sql = """
            SELECT
                Id,
                Name
            FROM `Groups`
            ORDER BY Name;
            """;

        return await connection.QueryAsync<Group>(sql);
    }

    public async Task<Group?> GetByIdAsync(int id)
    {
        using var connection = CreateConnection();

        const string sql = """
            SELECT
                Id,
                Name
            FROM `Groups`
            WHERE Id = @Id;
            """;

        return await connection
            .QueryFirstOrDefaultAsync<Group>(
                sql,
                new { Id = id });
    }

    public async Task<int> CreateAsync(Group group)
    {
        using var connection = CreateConnection();

        const string sql = """
            INSERT INTO `Groups`
            (
                Name
            )
            VALUES
            (
                @Name
            );

            SELECT LAST_INSERT_ID();
            """;

        return await connection.ExecuteScalarAsync<int>(
            sql,
            group);
    }

    public async Task<bool> UpdateAsync(Group group)
    {
        using var connection = CreateConnection();

        const string sql = """
            UPDATE `Groups`
            SET
                Name = @Name
            WHERE Id = @Id;
            """;

        int rowsAffected =
            await connection.ExecuteAsync(
                sql,
                group);

        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var connection = CreateConnection();

        const string sql = """
            DELETE FROM `Groups`
            WHERE Id = @Id;
            """;

        int rowsAffected =
            await connection.ExecuteAsync(
                sql,
                new { Id = id });

        return rowsAffected > 0;
    }
}