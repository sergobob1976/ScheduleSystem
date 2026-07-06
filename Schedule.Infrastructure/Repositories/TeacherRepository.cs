using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using Dapper;
using MySqlConnector;
using Microsoft.Extensions.Configuration;
using Schedule.Core.Interfaces;
using Schedule.Core.Models;

namespace Schedule.Infrastructure.Repositories;

public class TeacherRepository : ITeacherRepository
{
    private readonly string _connectionString;

    public TeacherRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Рядок підключення не знайдено.");
    }

    private IDbConnection CreateConnection() => new MySqlConnection(_connectionString);

    public async Task<IEnumerable<Teacher>> GetAllAsync()
    {
        using var connection = CreateConnection();
        string sql = "SELECT * FROM `Teachers`";
        return await connection.QueryAsync<Teacher>(sql);
    }

    public async Task<Teacher?> GetByIdAsync(int id)
    {
        using var connection = CreateConnection();
        string sql = "SELECT * FROM `Teachers` WHERE Id = @Id";
        return await connection.QueryFirstOrDefaultAsync<Teacher>(sql, new { Id = id });
    }

    public async Task<int> CreateAsync(Teacher teacher)
    {
        using var connection = CreateConnection();
        string sql = "INSERT INTO `Teachers` (Name, Position) VALUES (@Name, @Position); SELECT LAST_INSERT_ID();";
        return await connection.ExecuteScalarAsync<int>(sql, teacher);
    }

    public async Task<bool> UpdateAsync(Teacher teacher)
    {
        using var connection = CreateConnection();
        string sql = "UPDATE `Teachers` SET Name = @Name, Position = @Position WHERE Id = @Id";
        int rowsAffected = await connection.ExecuteAsync(sql, teacher);
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var connection = CreateConnection();
        string sql = "DELETE FROM `Teachers` WHERE Id = @Id";
        int rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
        return rowsAffected > 0;
    }
}