using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using MySqlConnector;
using Microsoft.Extensions.Configuration;
using Schedule.Core.Interfaces;
using Schedule.Core.Models;

namespace Schedule.Infrastructure.Repositories;

public class BaseLessonRepository : IBaseLessonRepository
{
    private readonly string _connectionString;

    public BaseLessonRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Рядок підключення не знайдено.");
    }

    private IDbConnection CreateConnection() => new MySqlConnection(_connectionString);

    private const string BaseJoinSql = @"
        SELECT 
            bl.*, g.*, t.*, d.*, c.*, s.*
        FROM `BaseLessons` bl
        INNER JOIN `Groups` g ON bl.GroupId = g.Id
        INNER JOIN `Teachers` t ON bl.TeacherId = t.Id
        INNER JOIN `Disciplines` d ON bl.DisciplineId = d.Id
        LEFT JOIN `ClassRooms` c ON bl.ClassRoomId = c.Id
        INNER JOIN `Semesters` s ON bl.SemesterId = s.Id";

    private readonly Func<BaseLesson, Group, Teacher, Discipline, ClassRoom, Semester, BaseLesson> _lessonMapper =
        (lesson, group, teacher, discipline, classroom, semester) =>
        {
            lesson.Group = group;
            lesson.Teacher = teacher;
            lesson.Discipline = discipline;
            lesson.ClassRoom = classroom;
            lesson.Semester = semester;
            return lesson;
        };

    public async Task<IEnumerable<BaseLesson>> GetAllAsync()
    {
        using var connection = CreateConnection();
        return await connection.QueryAsync<BaseLesson, Group, Teacher, Discipline, ClassRoom, Semester, BaseLesson>(
            BaseJoinSql, _lessonMapper, splitOn: "Id,Id,Id,Id,Id");
    }

    public async Task<BaseLesson?> GetByIdAsync(int id)
    {
        using var connection = CreateConnection();
        string sql = $"{BaseJoinSql} WHERE bl.Id = @Id";
        var results = await connection.QueryAsync<BaseLesson, Group, Teacher, Discipline, ClassRoom, Semester, BaseLesson>(
            sql, _lessonMapper, new { Id = id }, splitOn: "Id,Id,Id,Id,Id");
        return results.FirstOrDefault();
    }

    public async Task<IEnumerable<BaseLesson>> GetByGroupIdAsync(int groupId)
    {
        using var connection = CreateConnection();
        string sql = $"{BaseJoinSql} WHERE bl.GroupId = @GroupId ORDER BY bl.WeekDay, bl.LessonPosition";
        return await connection.QueryAsync<BaseLesson, Group, Teacher, Discipline, ClassRoom, Semester, BaseLesson>(
            sql, _lessonMapper, new { GroupId = groupId }, splitOn: "Id,Id,Id,Id,Id");
    }

    public async Task<int> CreateAsync(BaseLesson lesson)
    {
        using var connection = CreateConnection();
        string sql = @"
            INSERT INTO `BaseLessons` 
                (GroupId, TeacherId, DisciplineId, ClassRoomId, SemesterId, LessonPosition, WeekDay, WeekProperty) 
            VALUES 
                (@GroupId, @TeacherId, @DisciplineId, @ClassRoomId, @SemesterId, @LessonPosition, @WeekDay, @WeekProperty);
            SELECT LAST_INSERT_ID();";
        return await connection.ExecuteScalarAsync<int>(sql, lesson);
    }

    public async Task<bool> UpdateAsync(BaseLesson lesson)
    {
        using var connection = CreateConnection();
        string sql = @"
            UPDATE `BaseLessons` 
            SET GroupId = @GroupId, TeacherId = @TeacherId, DisciplineId = @DisciplineId, 
                ClassRoomId = @ClassRoomId, SemesterId = @SemesterId, 
                LessonPosition = @LessonPosition, WeekDay = @WeekDay, WeekProperty = @WeekProperty
            WHERE Id = @Id";
        int rowsAffected = await connection.ExecuteAsync(sql, lesson);
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var connection = CreateConnection();
        string sql = "DELETE FROM `BaseLessons` WHERE Id = @Id";
        int rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
        return rowsAffected > 0;
    }
}