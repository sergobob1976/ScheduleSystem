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

public class RealLessonRepository : IRealLessonRepository
{
    private readonly string _connectionString;

    public RealLessonRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Рядок підключення не знайдено.");
    }

    private IDbConnection CreateConnection() => new MySqlConnection(_connectionString);

    private const string BaseJoinSql = @"
        SELECT 
            rl.*, 
            g.*, 
            t.*, 
            d.*, 
            c.*, 
            s.*
        FROM `RealLessons` rl
        INNER JOIN `Groups` g ON rl.GroupId = g.Id
        INNER JOIN `Teachers` t ON rl.TeacherId = t.Id
        INNER JOIN `Disciplines` d ON rl.DisciplineId = d.Id
        LEFT JOIN `ClassRooms` c ON rl.ClassRoomId = c.Id
        INNER JOIN `Semesters` s ON rl.SemesterId = s.Id";

    private readonly Func<RealLesson, Group, Teacher, Discipline, ClassRoom, Semester, RealLesson> _lessonMapper =
        (lesson, group, teacher, discipline, classroom, semester) =>
        {
            lesson.Group = group;
            lesson.Teacher = teacher;
            lesson.Discipline = discipline;
            lesson.ClassRoom = classroom;
            lesson.Semester = semester;
            return lesson;
        };

    public async Task<IEnumerable<RealLesson>> GetAllAsync()
    {
        using var connection = CreateConnection();

        return await connection.QueryAsync<RealLesson, Group, Teacher, Discipline, ClassRoom, Semester, RealLesson>(
            BaseJoinSql,
            _lessonMapper,
            splitOn: "Id,Id,Id,Id,Id");
    }

    public async Task<RealLesson?> GetByIdAsync(int id)
    {
        using var connection = CreateConnection();

        string sql = $"{BaseJoinSql} WHERE rl.Id = @Id";

        var results = await connection.QueryAsync<RealLesson, Group, Teacher, Discipline, ClassRoom, Semester, RealLesson>(
            sql,
            _lessonMapper,
            new { Id = id },
            splitOn: "Id,Id,Id,Id,Id");

        return results.FirstOrDefault();
    }

    public async Task<IEnumerable<RealLesson>> GetByGroupIdAsync(int groupId)
    {
        using var connection = CreateConnection();

        string sql = $"{BaseJoinSql} WHERE rl.GroupId = @GroupId ORDER BY rl.LessonDate, rl.LessonPosition";

        return await connection.QueryAsync<RealLesson, Group, Teacher, Discipline, ClassRoom, Semester, RealLesson>(
            sql,
            _lessonMapper,
            new { GroupId = groupId },
            splitOn: "Id,Id,Id,Id,Id");
    }

    public async Task<IEnumerable<RealLesson>> GetByTeacherIdAsync(int teacherId)
    {
        using var connection = CreateConnection();

        string sql = $"{BaseJoinSql} WHERE rl.TeacherId = @TeacherId ORDER BY rl.LessonDate, rl.LessonPosition";

        return await connection.QueryAsync<RealLesson, Group, Teacher, Discipline, ClassRoom, Semester, RealLesson>(
            sql,
            _lessonMapper,
            new { TeacherId = teacherId },
            splitOn: "Id,Id,Id,Id,Id");
    }

    public async Task<int> CreateAsync(RealLesson lesson)
    {
        using var connection = CreateConnection();

        string sql = @"
            INSERT INTO `RealLessons` 
            (
                GroupId,
                TeacherId,
                DisciplineId,
                ClassRoomId,
                SemesterId,
                LessonDate,
                LessonPosition,
                WeekDay,
                WeekProperty,
                LessonType,
                ConferenceLink,
                ResourceLink
            ) 
            VALUES 
            (
                @GroupId,
                @TeacherId,
                @DisciplineId,
                @ClassRoomId,
                @SemesterId,
                @LessonDate,
                @LessonPosition,
                @WeekDay,
                @WeekProperty,
                @LessonType,
                @ConferenceLink,
                @ResourceLink
            );

            SELECT LAST_INSERT_ID();";

        return await connection.ExecuteScalarAsync<int>(sql, lesson);
    }

    public async Task<bool> UpdateAsync(RealLesson lesson)
    {
        using var connection = CreateConnection();

        string sql = @"
            UPDATE `RealLessons` 
            SET
                GroupId = @GroupId,
                TeacherId = @TeacherId,
                DisciplineId = @DisciplineId,
                ClassRoomId = @ClassRoomId,
                SemesterId = @SemesterId,
                LessonDate = @LessonDate,
                LessonPosition = @LessonPosition,
                WeekDay = @WeekDay,
                WeekProperty = @WeekProperty,
                LessonType = @LessonType,
                ConferenceLink = @ConferenceLink,
                ResourceLink = @ResourceLink
            WHERE Id = @Id";

        int rowsAffected = await connection.ExecuteAsync(sql, lesson);
        return rowsAffected > 0;
    }

    public async Task<bool> UpdateLinksAsync(int lessonId, string? conferenceLink, string? resourceLink)
    {
        using var connection = CreateConnection();

        string sql = @"
            UPDATE `RealLessons` 
            SET
                ConferenceLink = @ConferenceLink,
                ResourceLink = @ResourceLink
            WHERE Id = @Id";

        int rowsAffected = await connection.ExecuteAsync(
            sql,
            new
            {
                Id = lessonId,
                ConferenceLink = conferenceLink,
                ResourceLink = resourceLink
            });

        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var connection = CreateConnection();

        string sql = "DELETE FROM `RealLessons` WHERE Id = @Id";

        int rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
        return rowsAffected > 0;
    }
}