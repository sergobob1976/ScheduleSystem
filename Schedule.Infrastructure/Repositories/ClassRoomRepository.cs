using System.Data;
using Dapper;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using Schedule.Core.Interfaces;
using Schedule.Core.Models;

namespace Schedule.Infrastructure.Repositories;

public class ClassRoomRepository : IClassRoomRepository
{
    private readonly string _connectionString;

    public ClassRoomRepository(
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

    public async Task<IEnumerable<ClassRoom>> GetAllAsync()
    {
        using var connection = CreateConnection();

        const string sql = """
            SELECT
                Id,
                Name
            FROM `ClassRooms`
            ORDER BY Name;
            """;

        return await connection.QueryAsync<ClassRoom>(sql);
    }

    public async Task<ClassRoom?> GetByIdAsync(int id)
    {
        using var connection = CreateConnection();

        const string sql = """
            SELECT
                Id,
                Name
            FROM `ClassRooms`
            WHERE Id = @Id;
            """;

        return await connection
            .QueryFirstOrDefaultAsync<ClassRoom>(
                sql,
                new { Id = id });
    }

    public async Task<int> CreateAsync(
        ClassRoom classRoom)
    {
        using var connection = CreateConnection();

        const string sql = """
            INSERT INTO `ClassRooms`
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
            classRoom);
    }

    public async Task<bool> UpdateAsync(
        ClassRoom classRoom)
    {
        using var connection = CreateConnection();

        const string sql = """
            UPDATE `ClassRooms`
            SET
                Name = @Name
            WHERE Id = @Id;
            """;

        int rowsAffected =
            await connection.ExecuteAsync(
                sql,
                classRoom);

        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var connection = CreateConnection();

        const string sql = """
            DELETE FROM `ClassRooms`
            WHERE Id = @Id;
            """;

        int rowsAffected =
            await connection.ExecuteAsync(
                sql,
                new { Id = id });

        return rowsAffected > 0;
    }
}