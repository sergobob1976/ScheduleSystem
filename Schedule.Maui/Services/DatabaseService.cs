using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using Dapper;
using Schedule.Core.Models;
using Schedule.Core.Enums;
using Microsoft.Maui.Storage;

namespace Schedule.Maui.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService()
        {
            // Формуємо шлях до бази даних
            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "schedule.db3");
            _connectionString = $"Data Source={dbPath}";

            // === ТИМЧАСОВИЙ КОД ДЛЯ РОЗРОБКИ ТА ОЧИЩЕННЯ БАЗИ ===
            // Виводимо точний шлях у вікно Output у Visual Studio
            System.Diagnostics.Debug.WriteLine($"=========================================");
            System.Diagnostics.Debug.WriteLine($"ШЛЯХ ДО БАЗИ ДАНИХ SQLite: {dbPath}");
            System.Diagnostics.Debug.WriteLine($"=========================================");

            try
            {
                // Якщо стара база існує — видаляємо її, щоб примусово перестворити нову схему таблиць
                if (File.Exists(dbPath))
                {
                    File.Delete(dbPath);
                    System.Diagnostics.Debug.WriteLine("СТАРУ БАЗУ ДАНИХ УСПІШНО ВИДАЛЕНО ДЛЯ ОНОВЛЕННЯ СХЕМИ!");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Не вдалося видалити файл бази: {ex.Message}");
            }
            // ==================================================

            // Ініціалізуємо базу даних та створюємо нові таблиці
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using IDbConnection db = new SqliteConnection(_connectionString);

            // 1. Словники / Довідники (поля та типи строго за Core.Models)
            db.Execute("CREATE TABLE IF NOT EXISTS Groups (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL);");
            db.Execute("CREATE TABLE IF NOT EXISTS Disciplines (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL, TotalHours INTEGER NOT NULL);");
            db.Execute("CREATE TABLE IF NOT EXISTS Teachers (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL, Position TEXT);");
            db.Execute("CREATE TABLE IF NOT EXISTS ClassRooms (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL);");
            db.Execute("CREATE TABLE IF NOT EXISTS Semesters (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL);");

            // 2. Таблиця реальних занять (замість текстів зберігаємо ID та зв'язки)
            db.Execute(@"
                CREATE TABLE IF NOT EXISTS RealLessons (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    LessonDate TEXT NOT NULL, 
                    LessonPosition INTEGER NOT NULL,
                    GroupId INTEGER NOT NULL,
                    DisciplineId INTEGER NOT NULL,
                    TeacherId INTEGER NOT NULL,
                    ClassRoomId INTEGER NOT NULL,
                    SemesterId INTEGER NOT NULL,
                    FOREIGN KEY(GroupId) REFERENCES Groups(Id),
                    FOREIGN KEY(DisciplineId) REFERENCES Disciplines(Id),
                    FOREIGN KEY(TeacherId) REFERENCES Teachers(Id),
                    FOREIGN KEY(ClassRoomId) REFERENCES ClassRooms(Id),
                    FOREIGN KEY(SemesterId) REFERENCES Semesters(Id)
                );");
        }

        // ====================================================================
        // 1. Groups
        // ====================================================================
        public List<Group> GetGroups()
        {
            using IDbConnection db = new SqliteConnection(_connectionString);
            return [.. db.Query<Group>("SELECT * FROM Groups")];
        }

        public int SaveGroup(Group item)
        {
            using IDbConnection db = new SqliteConnection(_connectionString);
            if (item.Id > 0)
                return db.Execute("UPDATE Groups SET Name = @Name WHERE Id = @Id", item);

            return db.Execute("INSERT INTO Groups (Name) VALUES (@Name)", item);
        }

        public int DeleteGroup(Group item)
        {
            using IDbConnection db = new SqliteConnection(_connectionString);
            return db.Execute("DELETE FROM Groups WHERE Id = @Id", new { item.Id });
        }

        // ====================================================================
        // 2. Disciplines
        // ====================================================================
        public List<Discipline> GetDisciplines()
        {
            using IDbConnection db = new SqliteConnection(_connectionString);
            return [.. db.Query<Discipline>("SELECT * FROM Disciplines")];
        }

        public int SaveDiscipline(Discipline item)
        {
            using IDbConnection db = new SqliteConnection(_connectionString);
            if (item.Id > 0)
                return db.Execute("UPDATE Disciplines SET Name = @Name, TotalHours = @TotalHours WHERE Id = @Id", item);

            return db.Execute("INSERT INTO Disciplines (Name, TotalHours) VALUES (@Name, @TotalHours)", item);
        }

        public int DeleteDiscipline(Discipline item)
        {
            using IDbConnection db = new SqliteConnection(_connectionString);
            return db.Execute("DELETE FROM Disciplines WHERE Id = @Id", new { item.Id });
        }

        // ====================================================================
        // 3. Teachers
        // ====================================================================
        public List<Teacher> GetTeachers()
        {
            using IDbConnection db = new SqliteConnection(_connectionString);
            return [.. db.Query<Teacher>("SELECT * FROM Teachers")];
        }

        public int SaveTeacher(Teacher item)
        {
            using IDbConnection db = new SqliteConnection(_connectionString);
            if (item.Id > 0)
                return db.Execute("UPDATE Teachers SET Name = @Name, Position = @Position WHERE Id = @Id", item);

            return db.Execute("INSERT INTO Teachers (Name, Position) VALUES (@Name, @Position)", item);
        }

        public int DeleteTeacher(Teacher item)
        {
            using IDbConnection db = new SqliteConnection(_connectionString);
            return db.Execute("DELETE FROM Teachers WHERE Id = @Id", new { item.Id });
        }

        // ====================================================================
        // 4. ClassRooms
        // ====================================================================
        public List<ClassRoom> GetClassRooms()
        {
            using IDbConnection db = new SqliteConnection(_connectionString);
            return [.. db.Query<ClassRoom>("SELECT * FROM ClassRooms")];
        }

        public int SaveClassRoom(ClassRoom item)
        {
            using IDbConnection db = new SqliteConnection(_connectionString);
            if (item.Id > 0)
                return db.Execute("UPDATE ClassRooms SET Name = @Name WHERE Id = @Id", item);

            return db.Execute("INSERT INTO ClassRooms (Name) VALUES (@Name)", item);
        }

        public int DeleteClassRoom(ClassRoom item)
        {
            using IDbConnection db = new SqliteConnection(_connectionString);
            return db.Execute("DELETE FROM ClassRooms WHERE Id = @Id", new { item.Id });
        }

        // ====================================================================
        // 5. Semesters
        // ====================================================================
        public List<Semester> GetSemesters()
        {
            using IDbConnection db = new SqliteConnection(_connectionString);
            return [.. db.Query<Semester>("SELECT * FROM Semesters")];
        }

        public int SaveSemester(Semester item)
        {
            using IDbConnection db = new SqliteConnection(_connectionString);
            if (item.Id > 0)
                return db.Execute("UPDATE Semesters SET Name = @Name WHERE Id = @Id", item);

            return db.Execute("INSERT INTO Semesters (Name) VALUES (@Name)", item);
        }

        public int DeleteSemester(Semester item)
        {
            using IDbConnection db = new SqliteConnection(_connectionString);
            return db.Execute("DELETE FROM Semesters WHERE Id = @Id", new { item.Id });
        }

        // ====================================================================
        // 6. BaseLessons (Тимчасовий заглушка-метод для уникнення застережень)
        // ====================================================================
        public List<BaseLesson> GetBaseLessons()
        {
            return new List<BaseLesson>();
        }

        // ====================================================================
        // 7. RealLessons (Зв'язування об'єктів через Dapper Multi-Mapping)
        // ====================================================================
        public List<RealLesson> GetRealLessons()
        {
            using IDbConnection db = new SqliteConnection(_connectionString);

            string sql = @"
                SELECT 
                    l.Id, l.LessonDate, l.LessonPosition,
                    g.Id, g.Name,
                    d.Id, d.Name, d.TotalHours,
                    t.Id, t.Name, t.Position,
                    c.Id, c.Name,
                    s.Id, s.Name
                FROM RealLessons l
                LEFT JOIN Groups g ON l.GroupId = g.Id
                LEFT JOIN Disciplines d ON l.DisciplineId = d.Id
                LEFT JOIN Teachers t ON l.TeacherId = t.Id
                LEFT JOIN ClassRooms c ON l.ClassRoomId = c.Id
                LEFT JOIN Semesters s ON l.SemesterId = s.Id";

            var lessons = db.Query<RealLesson, Group, Discipline, Teacher, ClassRoom, Semester, RealLesson>(
                sql,
                (lesson, group, discipline, teacher, classRoom, semester) =>
                {
                    lesson.Group = group;
                    lesson.Discipline = discipline;
                    lesson.Teacher = teacher;
                    lesson.ClassRoom = classRoom;
                    lesson.Semester = semester;
                    return lesson;
                },
                splitOn: "Id,Id,Id,Id,Id"
            ).ToList();

            return lessons;
        }

        public int SaveRealLesson(RealLesson item)
        {
            using IDbConnection db = new SqliteConnection(_connectionString);

            var parameters = new
            {
                item.Id,
                LessonDate = item.LessonDate.ToString("yyyy-MM-dd HH:mm:ss"),
                item.LessonPosition,
                GroupId = item.Group?.Id ?? 0,
                DisciplineId = item.Discipline?.Id ?? 0,
                TeacherId = item.Teacher?.Id ?? 0,
                ClassRoomId = item.ClassRoom?.Id ?? 0,
                SemesterId = item.Semester?.Id ?? 0
            };

            if (item.Id > 0)
            {
                return db.Execute(@"
                    UPDATE RealLessons 
                    SET LessonDate = @LessonDate, LessonPosition = @LessonPosition, 
                        GroupId = @GroupId, DisciplineId = @DisciplineId, 
                        TeacherId = @TeacherId, ClassRoomId = @ClassRoomId, SemesterId = @SemesterId 
                    WHERE Id = @Id", parameters);
            }
            else
            {
                return db.Execute(@"
                    INSERT INTO RealLessons (LessonDate, LessonPosition, GroupId, DisciplineId, TeacherId, ClassRoomId, SemesterId) 
                    VALUES (@LessonDate, @LessonPosition, @GroupId, @DisciplineId, @TeacherId, @ClassRoomId, @SemesterId)", parameters);
            }
        }

        public int DeleteRealLesson(RealLesson item)
        {
            using IDbConnection db = new SqliteConnection(_connectionString);
            return db.Execute("DELETE FROM RealLessons WHERE Id = @Id", new { item.Id });
        }
    }
}