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

public class GroupDisciplineRepository
    : IGroupDisciplineRepository
{
    private readonly string _connectionString;

    private const string SelectSql = """
        SELECT
            gd.Id,
            gd.SemesterId,
            gd.GroupId,
            gd.DisciplineId,
            gd.LectureHours,
            gd.PracticalHours,
            gd.LaboratoryHours,
            gd.SeminarHours,
            gd.OtherHours,

            s.Id,
            s.Name,
            s.StartDate,
            s.EndDate,

            g.Id,
            g.Name,

            d.Id,
            d.SpecialtyId,
            d.Name,
            d.TotalHours,
            d.LectureHours,
            d.PracticalHours,
            d.LaboratoryHours,
            d.SeminarHours,
            d.OtherHours

        FROM `GroupDisciplines` gd

        INNER JOIN `Semesters` s
            ON s.Id = gd.SemesterId

        INNER JOIN `Groups` g
            ON g.Id = gd.GroupId

        INNER JOIN `Disciplines` d
            ON d.Id = gd.DisciplineId
        """;

    public GroupDisciplineRepository(
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

    public async Task<IEnumerable<GroupDiscipline>>
        GetAllAsync()
    {
        using var connection = CreateConnection();

        string sql = $"""
            {SelectSql}
            ORDER BY
                s.StartDate DESC,
                g.Name,
                d.Name;
            """;

        return await QueryAsync(connection, sql);
    }

    public async Task<IEnumerable<GroupDiscipline>>
        GetBySemesterIdAsync(int semesterId)
    {
        using var connection = CreateConnection();

        string sql = $"""
            {SelectSql}
            WHERE gd.SemesterId = @SemesterId
            ORDER BY g.Name, d.Name;
            """;

        return await QueryAsync(
            connection,
            sql,
            new { SemesterId = semesterId });
    }

    public async Task<IEnumerable<GroupDiscipline>>
        GetByGroupIdAsync(int groupId)
    {
        using var connection = CreateConnection();

        string sql = $"""
            {SelectSql}
            WHERE gd.GroupId = @GroupId
            ORDER BY s.StartDate DESC, d.Name;
            """;

        return await QueryAsync(
            connection,
            sql,
            new { GroupId = groupId });
    }

    public async Task<IEnumerable<GroupDiscipline>>
        GetBySemesterAndGroupAsync(
            int semesterId,
            int groupId)
    {
        using var connection = CreateConnection();

        string sql = $"""
            {SelectSql}
            WHERE
                gd.SemesterId = @SemesterId
                AND gd.GroupId = @GroupId
            ORDER BY d.Name;
            """;

        return await QueryAsync(
            connection,
            sql,
            new
            {
                SemesterId = semesterId,
                GroupId = groupId
            });
    }

    public async Task<GroupDiscipline?> GetByIdAsync(
        int id)
    {
        using var connection = CreateConnection();

        string sql = $"""
            {SelectSql}
            WHERE gd.Id = @Id;
            """;

        var result = await QueryAsync(
            connection,
            sql,
            new { Id = id });

        return result.FirstOrDefault();
    }

    public async Task<GroupDiscipline?>
        GetBySemesterGroupAndDisciplineAsync(
            int semesterId,
            int groupId,
            int disciplineId)
    {
        using var connection = CreateConnection();

        string sql = $"""
            {SelectSql}
            WHERE
                gd.SemesterId = @SemesterId
                AND gd.GroupId = @GroupId
                AND gd.DisciplineId = @DisciplineId;
            """;

        var result = await QueryAsync(
            connection,
            sql,
            new
            {
                SemesterId = semesterId,
                GroupId = groupId,
                DisciplineId = disciplineId
            });

        return result.FirstOrDefault();
    }

    public async Task<int> CreateAsync(
        GroupDiscipline groupDiscipline)
    {
        using var connection = CreateConnection();

        string sql = """
            INSERT INTO `GroupDisciplines`
            (
                SemesterId,
                GroupId,
                DisciplineId,
                LectureHours,
                PracticalHours,
                LaboratoryHours,
                SeminarHours,
                OtherHours
            )
            VALUES
            (
                @SemesterId,
                @GroupId,
                @DisciplineId,
                @LectureHours,
                @PracticalHours,
                @LaboratoryHours,
                @SeminarHours,
                @OtherHours
            );

            SELECT LAST_INSERT_ID();
            """;

        return await connection.ExecuteScalarAsync<int>(
            sql,
            groupDiscipline);
    }

    public async Task<bool> UpdateAsync(
        GroupDiscipline groupDiscipline)
    {
        using var connection = CreateConnection();

        string sql = """
            UPDATE `GroupDisciplines`
            SET
                SemesterId = @SemesterId,
                GroupId = @GroupId,
                DisciplineId = @DisciplineId,
                LectureHours = @LectureHours,
                PracticalHours = @PracticalHours,
                LaboratoryHours = @LaboratoryHours,
                SeminarHours = @SeminarHours,
                OtherHours = @OtherHours
            WHERE Id = @Id;
            """;

        int rowsAffected =
            await connection.ExecuteAsync(
                sql,
                groupDiscipline);

        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var connection = CreateConnection();

        string sql = """
            DELETE FROM `GroupDisciplines`
            WHERE Id = @Id;
            """;

        int rowsAffected =
            await connection.ExecuteAsync(
                sql,
                new { Id = id });

        return rowsAffected > 0;
    }

    private static async Task<IEnumerable<GroupDiscipline>>
        QueryAsync(
            IDbConnection connection,
            string sql,
            object? parameters = null)
    {
        return await connection.QueryAsync<
            GroupDiscipline,
            Semester,
            Group,
            Discipline,
            GroupDiscipline>(
            sql,
            (
                groupDiscipline,
                semester,
                group,
                discipline
            ) =>
            {
                groupDiscipline.Semester = semester;
                groupDiscipline.Group = group;
                groupDiscipline.Discipline = discipline;

                return groupDiscipline;
            },
            parameters,
            splitOn: "Id,Id,Id");
    }
}