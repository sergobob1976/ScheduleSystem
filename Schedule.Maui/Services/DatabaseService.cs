using Microsoft.Data.Sqlite;
using System.Data;
using Dapper;
using Schedule.Core.Models;

namespace Schedule.Maui.Services;

public class DatabaseService
{
    private readonly string _dbPath;

    public DatabaseService()
    {
        _dbPath = Path.Combine(FileSystem.AppDataDirectory, "schedule.db");
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using IDbConnection db = new SqliteConnection($"Data Source={_dbPath}");
        db.Open();

        db.Execute(@"
            CREATE TABLE IF NOT EXISTS Groups (
                Id INTEGER PRIMARY KEY,
                Name TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Teachers (
                Id INTEGER PRIMARY KEY,
                Name TEXT NOT NULL,
                Position TEXT
            );

            CREATE TABLE IF NOT EXISTS ClassRooms (
                Id INTEGER PRIMARY KEY,
                Name TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Disciplines (
                Id INTEGER PRIMARY KEY,
                Name TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS RealLessons (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                LessonPosition INTEGER NOT NULL,
                LessonDate TEXT NOT NULL,
                GroupId INTEGER,
                TeacherId INTEGER,
                ClassRoomId INTEGER,
                DisciplineId INTEGER
            );
        ");
    }

    public IEnumerable<RealLesson> GetRealLessons()
    {
        using IDbConnection db = new SqliteConnection($"Data Source={_dbPath}");
        try
        {
            string sql = @"
                SELECT l.*, g.*, t.*, c.*, d.*
                FROM RealLessons l
                LEFT JOIN Groups g ON l.GroupId = g.Id
                LEFT JOIN Teachers t ON l.TeacherId = t.Id
                LEFT JOIN ClassRooms c ON l.ClassRoomId = c.Id
                LEFT JOIN Disciplines d ON l.DisciplineId = d.Id";

            return db.Query<RealLesson, Group, Teacher, ClassRoom, Discipline, RealLesson>(
                sql,
                (lesson, group, teacher, classRoom, discipline) =>
                {
                    lesson.Group = group;
                    lesson.Teacher = teacher;
                    lesson.ClassRoom = classRoom;
                    lesson.Discipline = discipline;
                    return lesson;
                },
                splitOn: "Id,Id,Id,Id"
            );
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DB Error: {ex.Message}");
            return Enumerable.Empty<RealLesson>();
        }
    }

    public void SaveSyncedData(
        IEnumerable<Group> groups,
        IEnumerable<Teacher> teachers,
        IEnumerable<ClassRoom> classRooms,
        IEnumerable<Discipline> disciplines,
        IEnumerable<RealLesson> lessons)
    {
        using IDbConnection db = new SqliteConnection($"Data Source={_dbPath}");
        db.Open();
        using var transaction = db.BeginTransaction();

        try
        {
            // 1. Очищаємо старі дані
            db.Execute("DELETE FROM RealLessons", transaction: transaction);
            db.Execute("DELETE FROM Groups", transaction: transaction);
            db.Execute("DELETE FROM Teachers", transaction: transaction);
            db.Execute("DELETE FROM ClassRooms", transaction: transaction);
            db.Execute("DELETE FROM Disciplines", transaction: transaction);

            // 2. Вставляємо довідники (ВИПРАВЛЕНО під вашу модель Teacher)
            db.Execute("INSERT INTO Groups (Id, Name) VALUES (@Id, @Name)", groups, transaction: transaction);
            db.Execute("INSERT INTO Teachers (Id, Name, Position) VALUES (@Id, @Name, @Position)", teachers, transaction: transaction);
            db.Execute("INSERT INTO ClassRooms (Id, Name) VALUES (@Id, @Name)", classRooms, transaction: transaction);
            db.Execute("INSERT INTO Disciplines (Id, Name) VALUES (@Id, @Name)", disciplines, transaction: transaction);

            // 3. Вставляємо уроки
            var lessonParameters = lessons.Select(l => new
            {
                l.LessonPosition,
                LessonDate = l.LessonDate.ToString("yyyy-MM-dd HH:mm:ss"),
                GroupId = l.Group?.Id,
                TeacherId = l.Teacher?.Id,
                ClassRoomId = l.ClassRoom?.Id,
                DisciplineId = l.Discipline?.Id
            });

            db.Execute(@"
                INSERT INTO RealLessons (LessonPosition, LessonDate, GroupId, TeacherId, ClassRoomId, DisciplineId) 
                VALUES (@LessonPosition, @LessonDate, @GroupId, @TeacherId, @ClassRoomId, @DisciplineId)",
                lessonParameters, transaction: transaction);

            transaction.Commit();
        }
        catch (Exception)
        {
            transaction.Rollback();
            throw;
        }
    }
}