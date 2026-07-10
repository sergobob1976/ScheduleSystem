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

public class GroupSpecialtyRepository
    : IGroupSpecialtyRepository
{
    private readonly string _connectionString;

    private const string SelectSql = """
        SELECT
            gs.Id,
            gs.GroupId,
            gs.SpecialtyId,

            g.Id,
            g.Name,

            s.Id,
            s.Code,
            s.Name

        FROM `GroupSpecialties` gs

        INNER JOIN `Groups` g
            ON g.Id = gs.GroupId

        INNER JOIN `Specialties` s
            ON s.Id = gs.SpecialtyId
        """;

    public GroupSpecialtyRepository(
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

    public async Task<IEnumerable<GroupSpecialty>>
        GetAllAsync()
    {
        using var connection = CreateConnection();

        string sql = $"""
            {SelectSql}
            ORDER BY g.Name, s.Code, s.Name;
            """;

        return await QueryAsync(connection, sql);
    }

    public async Task<IEnumerable<GroupSpecialty>>
        GetByGroupIdAsync(int groupId)
    {
        using var connection = CreateConnection();

        string sql = $"""
            {SelectSql}
            WHERE gs.GroupId = @GroupId
            ORDER BY s.Code, s.Name;
            """;

        return await QueryAsync(
            connection,
            sql,
            new { GroupId = groupId });
    }

    public async Task<IEnumerable<GroupSpecialty>>
        GetBySpecialtyIdAsync(int specialtyId)
    {
        using var connection = CreateConnection();

        string sql = $"""
            {SelectSql}
            WHERE gs.SpecialtyId = @SpecialtyId
            ORDER BY g.Name;
            """;

        return await QueryAsync(
            connection,
            sql,
            new { SpecialtyId = specialtyId });
    }

    public async Task<GroupSpecialty?> GetByIdAsync(
        int id)
    {
        using var connection = CreateConnection();

        string sql = $"""
            {SelectSql}
            WHERE gs.Id = @Id;
            """;

        var result = await QueryAsync(
            connection,
            sql,
            new { Id = id });

        return result.FirstOrDefault();
    }

    public async Task<GroupSpecialty?> GetExistingAsync(
        int groupId,
        int specialtyId)
    {
        using var connection = CreateConnection();

        string sql = $"""
            {SelectSql}
            WHERE
                gs.GroupId = @GroupId
                AND gs.SpecialtyId = @SpecialtyId;
            """;

        var result = await QueryAsync(
            connection,
            sql,
            new
            {
                GroupId = groupId,
                SpecialtyId = specialtyId
            });

        return result.FirstOrDefault();
    }

    public async Task<int> CreateAsync(
        GroupSpecialty groupSpecialty)
    {
        using var connection = CreateConnection();

        const string sql = """
            INSERT INTO `GroupSpecialties`
            (
                GroupId,
                SpecialtyId
            )
            VALUES
            (
                @GroupId,
                @SpecialtyId
            );

            SELECT LAST_INSERT_ID();
            """;

        return await connection.ExecuteScalarAsync<int>(
            sql,
            groupSpecialty);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var connection = CreateConnection();

        const string sql = """
            DELETE FROM `GroupSpecialties`
            WHERE Id = @Id;
            """;

        int rowsAffected =
            await connection.ExecuteAsync(
                sql,
                new { Id = id });

        return rowsAffected > 0;
    }

    private static async Task<IEnumerable<GroupSpecialty>>
        QueryAsync(
            IDbConnection connection,
            string sql,
            object? parameters = null)
    {
        return await connection.QueryAsync<
            GroupSpecialty,
            Group,
            Specialty,
            GroupSpecialty>(
            sql,
            (
                groupSpecialty,
                group,
                specialty
            ) =>
            {
                groupSpecialty.Group = group;
                groupSpecialty.Specialty = specialty;

                return groupSpecialty;
            },
            parameters,
            splitOn: "Id,Id");
    }
}
