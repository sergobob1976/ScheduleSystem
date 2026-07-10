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

public class SpecialtyRepository : ISpecialtyRepository
{
    private readonly string _connectionString;

    public SpecialtyRepository(
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

    public async Task<IEnumerable<Specialty>> GetAllAsync()
    {
        using var connection = CreateConnection();

        const string sql = """
            SELECT
                Id,
                Code,
                Name
            FROM `Specialties`
            ORDER BY Code, Name;
            """;

        return await connection.QueryAsync<Specialty>(sql);
    }

    public async Task<Specialty?> GetByIdAsync(int id)
    {
        using var connection = CreateConnection();

        const string sql = """
            SELECT
                Id,
                Code,
                Name
            FROM `Specialties`
            WHERE Id = @Id;
            """;

        return await connection
            .QueryFirstOrDefaultAsync<Specialty>(
                sql,
                new { Id = id });
    }

    public async Task<int> CreateAsync(
        Specialty specialty)
    {
        using var connection = CreateConnection();

        const string sql = """
            INSERT INTO `Specialties`
            (
                Code,
                Name
            )
            VALUES
            (
                @Code,
                @Name
            );

            SELECT LAST_INSERT_ID();
            """;

        return await connection.ExecuteScalarAsync<int>(
            sql,
            specialty);
    }

    public async Task<bool> UpdateAsync(
        Specialty specialty)
    {
        using var connection = CreateConnection();

        const string sql = """
            UPDATE `Specialties`
            SET
                Code = @Code,
                Name = @Name
            WHERE Id = @Id;
            """;

        int rowsAffected =
            await connection.ExecuteAsync(
                sql,
                specialty);

        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var connection = CreateConnection();

        const string sql = """
            DELETE FROM `Specialties`
            WHERE Id = @Id;
            """;

        int rowsAffected =
            await connection.ExecuteAsync(
                sql,
                new { Id = id });

        return rowsAffected > 0;
    }
}