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

public class DisciplineRepository : IDisciplineRepository
{
    private readonly string _connectionString;

    public DisciplineRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Рядок підключення не знайдено.");
    }

    private IDbConnection CreateConnection() => new MySqlConnection(_connectionString);

    public async Task<IEnumerable<Discipline>> GetAllAsync()
    {
        using var connection = CreateConnection();
        string sql = "SELECT * FROM `Disciplines`";
        return await connection.QueryAsync<Discipline>(sql);
    }

    public async Task<Discipline?> GetByIdAsync(int id)
    {
        using var connection = CreateConnection();
        string sql = "SELECT * FROM `Disciplines` WHERE Id = @Id";
        return await connection.QueryFirstOrDefaultAsync<Discipline>(sql, new { Id = id });
    }

    public async Task<int> CreateAsync(Discipline discipline)
    {
        using var connection = CreateConnection();

        string sql = @"
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
            @TotalHours,
            @LectureHours,
            @PracticalHours,
            @LaboratoryHours,
            @SeminarHours,
            @OtherHours
        );

        SELECT LAST_INSERT_ID();
    ";

        return await connection.ExecuteScalarAsync<int>(sql, discipline);
    }

    public async Task<bool> UpdateAsync(Discipline discipline)
    {
        using var connection = CreateConnection();

        string sql = @"
        UPDATE `Disciplines`
        SET
            SpecialtyId = @SpecialtyId,
            Name = @Name,
            TotalHours = @TotalHours,
            LectureHours = @LectureHours,
            PracticalHours = @PracticalHours,
            LaboratoryHours = @LaboratoryHours,
            SeminarHours = @SeminarHours,
            OtherHours = @OtherHours
        WHERE Id = @Id;
    ";

        int rowsAffected = await connection.ExecuteAsync(sql, discipline);
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var connection = CreateConnection();
        string sql = "DELETE FROM `Disciplines` WHERE Id = @Id";
        int rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
        return rowsAffected > 0;
    }
}