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
        // Кросплатформний шлях до файлу локальної бази даних SQLite. 
        _dbPath = Path.Combine(FileSystem.AppDataDirectory, "schedule_v2.db");
        InitializeDatabase();
    }

    /// <summary>
    /// Ініціалізація бази даних: створення таблиць, якщо вони не існують
    /// </summary>
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
                DisciplineId INTEGER,
                FOREIGN KEY(GroupId) REFERENCES Groups(Id),
                FOREIGN KEY(TeacherId) REFERENCES Teachers(Id),
                FOREIGN KEY(ClassRoomId) REFERENCES ClassRooms(Id),
                FOREIGN KEY(DisciplineId) REFERENCES Disciplines(Id)
            );
        ");
    }

    /// <summary>
    /// Очищення старих даних та збереження нових сутностей, отриманих через API
    /// </summary>
    public void SaveSyncedData(List<Group> groups, List<Teacher> teachers, List<ClassRoom> classRooms, List<Discipline> disciplines, List<RealLesson> lessons)
    {
        using IDbConnection db = new SqliteConnection($"Data Source={_dbPath}");
        db.Open();
        using var transaction = db.BeginTransaction();

        try
        {
            // 1. Спочатку очищаємо старі локальні довідники
            db.Execute("DELETE FROM Groups", transaction: transaction);
            db.Execute("DELETE FROM Teachers", transaction: transaction);
            db.Execute("DELETE FROM ClassRooms", transaction: transaction);
            db.Execute("DELETE FROM Disciplines", transaction: transaction);
            db.Execute("DELETE FROM RealLessons", transaction: transaction);

            // 2. Вставляємо свіжі довідники з сервера
            db.Execute("INSERT INTO Groups (Id, Name) VALUES (@Id, @Name)", groups, transaction: transaction);
            db.Execute("INSERT INTO Teachers (Id, Name, Position) VALUES (@Id, @Name, @Position)", teachers, transaction: transaction);
            db.Execute("INSERT INTO ClassRooms (Id, Name) VALUES (@Id, @Name)", classRooms, transaction: transaction);
            db.Execute("INSERT INTO Disciplines (Id, Name) VALUES (@Id, @Name)", disciplines, transaction: transaction);

            // 3. Формуємо параметри для збереження розкладу
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

    /// <summary>
    /// Завантаження занять з локальної бази даних SQLite
    /// </summary>
    public List<RealLesson> GetRealLessons()
    {
        using IDbConnection db = new SqliteConnection($"Data Source={_dbPath}");
        db.Open();

        // Запит з Dapper, який зв'язує таблицю уроків із довідниками груп, викладачів тощо.
        string sql = @"
            SELECT r.*, g.*, t.*, c.*, d.*
            FROM RealLessons r
            LEFT JOIN Groups g ON r.GroupId = g.Id
            LEFT JOIN Teachers t ON r.TeacherId = t.Id
            LEFT JOIN ClassRooms c ON r.ClassRoomId = c.Id
            LEFT JOIN Disciplines d ON r.DisciplineId = d.Id";

        var lessons = db.Query<RealLesson, Group, Teacher, ClassRoom, Discipline, RealLesson>(
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
        ).ToList();

        return lessons;
    }

    /// <summary>
    /// Завантаження списку груп з локальної бази даних SQLite
    /// </summary>
    public List<Group> GetGroups()
    {
        using IDbConnection db = new SqliteConnection($"Data Source={_dbPath}");
        db.Open();
        return db.Query<Group>("SELECT * FROM Groups ORDER BY Name").ToList();
    }
}