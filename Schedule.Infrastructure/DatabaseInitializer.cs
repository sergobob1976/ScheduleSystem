using System.Data;
using Dapper;
using MySqlConnector;
using Microsoft.Extensions.Configuration;

namespace Schedule.Infrastructure;

public class DatabaseInitializer
{
    private readonly string _connectionString;

    public DatabaseInitializer(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Рядок підключення не знайдено.");
    }

    public void Initialize()
    {
        using IDbConnection connection = new MySqlConnection(_connectionString);
        connection.Open();

        // Повний скрипт для створення всієї структури бази даних MySQL
        string createTablesSql = @"
            -- 1. Таблиця Користувачів (Адміністратори/Диспетчери)
            CREATE TABLE IF NOT EXISTS `Users` (
                `Id` INT AUTO_INCREMENT PRIMARY KEY,
                `Username` VARCHAR(50) NOT NULL UNIQUE,
                `PasswordHash` VARCHAR(255) NOT NULL,
                `Role` VARCHAR(20) NOT NULL
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

            -- 2. Таблиця Груп
            CREATE TABLE IF NOT EXISTS `Groups` (
                `Id` INT AUTO_INCREMENT PRIMARY KEY,
                `Name` VARCHAR(50) NOT NULL UNIQUE
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

            -- 3. Таблиця Викладачів
            CREATE TABLE IF NOT EXISTS `Teachers` (
                `Id` INT AUTO_INCREMENT PRIMARY KEY,
                `FirstName` VARCHAR(50) NOT NULL,
                `LastName` VARCHAR(50) NOT NULL,
                `MiddleName` VARCHAR(50) NULL
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

            -- 4. Таблиця Предметів (Дисциплін)
            CREATE TABLE IF NOT EXISTS `Disciplines` (
                `Id` INT AUTO_INCREMENT PRIMARY KEY,
                `Name` VARCHAR(150) NOT NULL UNIQUE
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

            -- 5. Таблиця Аудиторій (Кабінетів)
            CREATE TABLE IF NOT EXISTS `ClassRooms` (
                `Id` INT AUTO_INCREMENT PRIMARY KEY,
                `Name` VARCHAR(50) NOT NULL UNIQUE
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

            -- 6. Таблиця Семестрів (Навчальних періодів)
            CREATE TABLE IF NOT EXISTS `Semesters` (
                `Id` INT AUTO_INCREMENT PRIMARY KEY,
                `Name` VARCHAR(100) NOT NULL,
                `StartDate` DATE NOT NULL,
                `EndDate` DATE NOT NULL
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

            -- 7. Головна таблиця Розкладу (Пари / Реальні уроки)
            CREATE TABLE IF NOT EXISTS `RealLessons` (
                `Id` INT AUTO_INCREMENT PRIMARY KEY,
                `GroupId` INT NOT NULL,
                `TeacherId` INT NOT NULL,
                `DisciplineId` INT NOT NULL,
                `ClassRoomId` INT NULL,
                `SemesterId` INT NOT NULL,
                `LessonDate` DATE NOT NULL,              -- Конкретна дата проведення пари
                `LessonPosition` INT NOT NULL,           -- Номер пари (1, 2, 3...)
                `WeekDay` INT NOT NULL,                  -- День тижня (1 = Понеділок тощо)
                `WeekProperty` INT NOT NULL,             -- Чисельник / Знаменник / Кожен тиждень
                CONSTRAINT `FK_Lessons_Group` FOREIGN KEY (`GroupId`) REFERENCES `Groups`(`Id`) ON DELETE CASCADE,
                CONSTRAINT `FK_Lessons_Teacher` FOREIGN KEY (`TeacherId`) REFERENCES `Teachers`(`Id`) ON DELETE CASCADE,
                CONSTRAINT `FK_Lessons_Discipline` FOREIGN KEY (`DisciplineId`) REFERENCES `Disciplines`(`Id`) ON DELETE CASCADE,
                CONSTRAINT `FK_Lessons_ClassRoom` FOREIGN KEY (`ClassRoomId`) REFERENCES `ClassRooms`(`Id`) ON DELETE SET NULL,
                CONSTRAINT `FK_Lessons_Semester` FOREIGN KEY (`SemesterId`) REFERENCES `Semesters`(`Id`) ON DELETE CASCADE
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
        ";

        connection.Execute(createTablesSql);
    }
}