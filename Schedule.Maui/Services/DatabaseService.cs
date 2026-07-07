using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using Dapper;
using Schedule.Core.Models; // Використовуємо класи з нашої Core-бібліотеки
using Schedule.Core.Enums;

namespace Schedule.Maui.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService()
        {
            // Формуємо шлях до бази даних у локальному сховищі пристрою
            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "schedule.db3");
            _connectionString = $"Data Source={dbPath}";

            // Ініціалізуємо базу даних та створюємо таблиці, якщо їх немає
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using IDbConnection db = new SqliteConnection(_connectionString);

            // 1. Словники / Довідники (поля та типи строго за Core.Models)
            db.Execute("CREATE TABLE IF NOT EXISTS Groups (GroupName TEXT PRIMARY KEY);");
            db.Execute("CREATE TABLE IF NOT EXISTS Disciplines (DisciplineName TEXT PRIMARY KEY, TotalHours INTEGER);");
            db.Execute("CREATE TABLE IF NOT EXISTS Teachers (TeacherName TEXT PRIMARY KEY, Position TEXT);");
            db.Execute("CREATE TABLE IF NOT EXISTS ClassRooms (ClassRoomName TEXT PRIMARY KEY);");
            db.Execute("CREATE TABLE IF NOT EXISTS LessonPositions (LessonPositionNumber INTEGER PRIMARY KEY);");
            db.Execute("CREATE TABLE IF NOT EXISTS WeekDays (WeekDayName TEXT PRIMARY KEY);");
            db.Execute("CREATE TABLE IF NOT EXISTS Semesters (SemesterName TEXT PRIMARY KEY);");
            db.Execute("CREATE TABLE IF NOT EXISTS WeekProperties (WeekPropertyName TEXT PRIMARY KEY);");

            // 2. Таблиця Користувачів (Точно під User.cs + Password для автентифікації)
            db.Execute(@"
                CREATE TABLE IF NOT EXISTS Users (
                    Username TEXT PRIMARY KEY,
                    Message TEXT,
                    Role INTEGER,      -- Енуми в SQLite зберігаються як INTEGER (числа)
                    IsLoggedIn INTEGER, -- bool зберігається як 0 або 1
                    Password TEXT       -- Поле для авторизації
                );");

            // 3. Основні таблиці розкладу (Точно під BaseLesson.cs та RealLesson.cs)
            db.Execute(@"
                CREATE TABLE IF NOT EXISTS BaseLessons (
                    RealLesson_id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Semester TEXT,
                    WeekDay TEXT,
                    [Group] TEXT,
                    LessonPosition INTEGER,
                    Discipline TEXT,
                    Teacher TEXT,
                    WeekProperty TEXT,
                    ClassRoom TEXT
                );");

            db.Execute(@"
                CREATE TABLE IF NOT EXISTS RealLessons (
                    RealLesson_id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Semester TEXT,
                    Date TEXT,
                    [Group] TEXT,
                    LessonPosition INTEGER,
                    Discipline TEXT,
                    Teacher TEXT,
                    WeekProperty TEXT,
                    ClassRoom TEXT,
                    TeacherLink TEXT
                );");
        }

        // ================= РОБОТА З КОРИСТУВАЧАМИ (User) =================

        public List<User> GetUsers()
        {
            using IDbConnection db = new SqliteConnection(_connectionString);
            return db.Query<User>("SELECT * FROM Users").ToList();
        }

        public int SaveUser(User user, string password = "")
        {
            using IDbConnection db = new SqliteConnection(_connectionString);

            // Перевіряємо, чи вже є користувач із таким логіном
            var exists = db.ExecuteScalar<int>("SELECT COUNT(1) FROM Users WHERE Username = @Username", new { user.Username });

            if (exists > 0)
            {
                // Оновлення (пароль не зачіпаємо при звичайному оновленні профілю)
                return db.Execute(@"
                    UPDATE Users 
                    SET Message = @Message, Role = @Role, IsLoggedIn = @IsLoggedIn 
                    WHERE Username = @Username",
                    new { user.Username, user.Message, Role = (int)user.Role, user.IsLoggedIn });
            }
            else
            {
                // Вставка нового користувача разом із паролем
                return db.Execute(@"
                    INSERT INTO Users (Username, Message, Role, IsLoggedIn, Password) 
                    VALUES (@Username, @Message, @Role, @IsLoggedIn, @Password)",
                    new { user.Username, user.Message, Role = (int)user.Role, user.IsLoggedIn, Password = password });
            }
        }

        public int DeleteUser(User user)
        {
            using IDbConnection db = new SqliteConnection(_connectionString);
            return db.Execute("DELETE FROM Users WHERE Username = @Username", new { user.Username });
        }


        // ================= РОБОТА З БАЗОВИМ РОЗКЛАДОМ (BaseLesson) =================

        public List<BaseLesson> GetBaseLessons()
        {
            using IDbConnection db = new SqliteConnection(_connectionString);
            return db.Query<BaseLesson>("SELECT * FROM BaseLessons").ToList();
        }

        public int SaveBaseLesson(BaseLesson lesson)
        {
            using IDbConnection db = new SqliteConnection(_connectionString);

            if (lesson.RealLesson_id != 0)
            {
                return db.Execute(@"
                    UPDATE BaseLessons 
                    SET Semester = @Semester, WeekDay = @WeekDay, [Group] = @Group, 
                        LessonPosition = @LessonPosition, Discipline = @Discipline, 
                        Teacher = @Teacher, WeekProperty = @WeekProperty, ClassRoom = @ClassRoom 
                    WHERE RealLesson_id = @RealLesson_id", lesson);
            }

            return db.Execute(@"
                INSERT INTO BaseLessons (Semester, WeekDay, [Group], LessonPosition, Discipline, Teacher, WeekProperty, ClassRoom) 
                VALUES (@Semester, @WeekDay, @Group, @LessonPosition, @Discipline, @Teacher, @WeekProperty, @ClassRoom)", lesson);
        }

        public int DeleteBaseLesson(BaseLesson lesson)
        {
            using IDbConnection db = new SqliteConnection(_connectionString);
            return db.Execute("DELETE FROM BaseLessons WHERE RealLesson_id = @RealLesson_id", new { lesson.RealLesson_id });
        }


        // ================= РОБОТА З РЕАЛЬНИМ РОЗКЛАДОМ (RealLesson) =================

        public List<RealLesson> GetRealLessons()
        {
            using IDbConnection db = new SqliteConnection(_connectionString);
            return db.Query<RealLesson>("SELECT * FROM RealLessons").ToList();
        }

        public int SaveRealLesson(RealLesson lesson)
        {
            using IDbConnection db = new SqliteConnection(_connectionString);

            if (lesson.RealLesson_id != 0)
            {
                return db.Execute(@"
                    UPDATE RealLessons 
                    SET Semester = @Semester, Date = @Date, [Group] = @Group, 
                        LessonPosition = @LessonPosition, Discipline = @Discipline, 
                        Teacher = @Teacher, WeekProperty = @WeekProperty, ClassRoom = @ClassRoom, 
                        TeacherLink = @TeacherLink 
                    WHERE RealLesson_id = @RealLesson_id", lesson);
            }

            return db.Execute(@"
                INSERT INTO RealLessons (Semester, Date, [Group], LessonPosition, Discipline, Teacher, WeekProperty, ClassRoom, TeacherLink) 
                VALUES (@Semester, @Date, @Group, @LessonPosition, @Discipline, @Teacher, @WeekProperty, @ClassRoom, @TeacherLink)", lesson);
        }

        public int DeleteRealLesson(RealLesson lesson)
        {
            using IDbConnection db = new SqliteConnection(_connectionString);
            return db.Execute("DELETE FROM RealLessons WHERE RealLesson_id = @RealLesson_id", new { lesson.RealLesson_id });
        }


        // ================= CRUD МЕТОДИ ДЛЯ ДОВІДНИКІВ =================

        // 1. Groups
        public List<Group> GetGroups()
        {
            using IDbConnection db = new SqliteConnection(_connectionString);
            return db.Query<Group>("SELECT * FROM Groups").ToList();
        }
        public int SaveGroup(Group item)
        {
            using IDbConnection db = new SqliteConnection(_connectionString);
            return db.Execute("INSERT OR IGNORE INTO Groups (GroupName) VALUES (@GroupName)", item);
        }
        public int DeleteGroup(Group item)
        {
            using IDbConnection db = new SqliteConnection(_connectionString);
            return db.Execute("DELETE FROM Groups WHERE GroupName = @GroupName", new { item.GroupName });
        }

        // 2. Disciplines
        public List<Discipline> GetDisciplines()
        {
            using IDbConnection db = new SqliteConnection(_connectionString);
            return db.Query<Discipline>("SELECT * FROM Disciplines").ToList();
        }
        public int SaveDiscipline(Discipline item)
        {
            using IDbConnection db = new SqliteConnection(_connectionString);
            var exists = db.ExecuteScalar<int>("SELECT COUNT(1) FROM Disciplines WHERE DisciplineName = @DisciplineName", new { item.DisciplineName });
            if (exists > 0)
                return db.Execute("UPDATE Disciplines SET TotalHours = @TotalHours WHERE DisciplineName = @DisciplineName", item);
            return db.Execute("INSERT INTO Disciplines (DisciplineName, TotalHours) VALUES (@DisciplineName, @TotalHours)", item);
        }
        public int DeleteDiscipline(Discipline item)
        {
            using IDbConnection db = new SqliteConnection(_connectionString);
            return db.Execute("DELETE FROM Disciplines WHERE DisciplineName = @DisciplineName", new { item.DisciplineName });
        }

        // 3. Teachers
        public List<Teacher> GetTeachers()
        {
            using IDbConnection db = new SqliteConnection(_connectionString);
            return db.Query<Teacher>("SELECT * FROM Teachers").ToList();
        }
        public int SaveTeacher(Teacher item)
        {
            using IDbConnection db = new SqliteConnection(_connectionString);
            var exists = db.ExecuteScalar<int>("SELECT COUNT(1) FROM Teachers WHERE TeacherName = @TeacherName", new { item.TeacherName });
            if (exists > 0)
                return db.Execute("UPDATE Teachers SET Position = @Position WHERE TeacherName = @TeacherName", item);
            return db.Execute("INSERT INTO Teachers (TeacherName, Position) VALUES (@TeacherName, @Position)", item);
        }
        public int DeleteTeacher(Teacher item)
        {
            using IDbConnection db = new SqliteConnection(_connectionString);
            return db.Execute("DELETE FROM Teachers WHERE TeacherName = @TeacherName", new { item.TeacherName });
        }

        // 4. ClassRooms
        public List<ClassRoom> GetClassRooms()
        {
            using IDbConnection db = new SqliteConnection(_connectionString);
            return db.Query<ClassRoom>("SELECT * FROM ClassRooms").ToList();
        }
        public int SaveClassRoom(ClassRoom item)
        {
            using IDbConnection db = new SqliteConnection(_connectionString);
            return db.Execute("INSERT OR IGNORE INTO ClassRooms (ClassRoomName) VALUES (@ClassRoomName)", item);
        }
        public int DeleteClassRoom(ClassRoom item)
        {
            using IDbConnection db = new SqliteConnection(_connectionString);
            return db.Execute("DELETE FROM ClassRooms WHERE ClassRoomName = @ClassRoomName", new { item.ClassRoomName });
        }

        // 5. LessonPositions
        public List<LessonPosition> GetLessonPositions()
        {
            using IDbConnection db = new SqliteConnection(_connectionString);
            return db.Query<LessonPosition>("SELECT * FROM LessonPositions").ToList();
        }
        public int SaveLessonPosition(LessonPosition item)
        {
            using IDbConnection db = new SqliteConnection(_connectionString);
            return db.Execute("INSERT OR IGNORE INTO LessonPositions (LessonPositionNumber) VALUES (@LessonPositionNumber)", item);
        }
        public int DeleteLessonPosition(LessonPosition item)
        {
            using IDbConnection db = new SqliteConnection(_connectionString);
            return db.Execute("DELETE FROM LessonPositions WHERE LessonPositionNumber = @LessonPositionNumber", new { item.LessonPositionNumber });
        }

        // 6. WeekDays
        public List<WeekDay> GetWeekDays()
        {
            using IDbConnection db = new SqliteConnection(_connectionString);
            return db.Query<WeekDay>("SELECT * FROM WeekDays").ToList();
        }
        public int SaveWeekDay(WeekDay item)
        {
            using IDbConnection db = new SqliteConnection(_connectionString);
            return db.Execute("INSERT OR IGNORE INTO WeekDays (WeekDayName) VALUES (@WeekDayName)", item);
        }
        public int DeleteWeekDay(WeekDay item)
        {
            using IDbConnection db = new SqliteConnection(_connectionString);
            return db.Execute("DELETE FROM WeekDays WHERE WeekDayName = @WeekDayName", new { item.WeekDayName });
        }

        // 7. Semesters
        public List<Semester> GetSemesters()
        {
            using IDbConnection db = new SqliteConnection(_connectionString);
            return db.Query<Semester>("SELECT * FROM Semesters").ToList();
        }
        public int SaveSemester(Semester item)
        {
            using IDbConnection db = new SqliteConnection(_connectionString);
            return db.Execute("INSERT OR IGNORE INTO Semesters (SemesterName) VALUES (@SemesterName)", item);
        }
        public int DeleteSemester(Semester item)
        {
            using IDbConnection db = new SqliteConnection(_connectionString);
            return db.Execute("DELETE FROM Semesters WHERE SemesterName = @SemesterName", new { item.SemesterName });
        }

        // 8. WeekProperties
        public List<WeekProperty> GetWeekProperties()
        {
            using IDbConnection db = new SqliteConnection(_connectionString);
            return db.Query<WeekProperty>("SELECT * FROM WeekProperties").ToList();
        }
        public int SaveWeekProperty(WeekProperty item)
        {
            using IDbConnection db = new SqliteConnection(_connectionString);
            return db.Execute("INSERT OR IGNORE INTO WeekProperties (WeekPropertyName) VALUES (@WeekPropertyName)", item);
        }
        public int DeleteWeekProperty(WeekProperty item)
        {
            using IDbConnection db = new SqliteConnection(_connectionString);
            return db.Execute("DELETE FROM WeekProperties WHERE WeekPropertyName = @WeekPropertyName", new { item.WeekPropertyName });
        }
    }
}