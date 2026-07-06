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

        // На цьому етапі створюємо лише ті таблиці, з якими вже вміємо працювати
        string createTablesSql = @"
            -- 1. Таблиця Груп (вже працює)
            CREATE TABLE IF NOT EXISTS `Groups` (
                `Id` INT AUTO_INCREMENT PRIMARY KEY,
                `Name` VARCHAR(50) NOT NULL UNIQUE
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

            -- 2. Таблиця Викладачів (додаємо зараз)
            CREATE TABLE IF NOT EXISTS `Teachers` (
                `Id` INT AUTO_INCREMENT PRIMARY KEY,
                `Name` VARCHAR(100) NOT NULL UNIQUE,
                `Position` VARCHAR(100) NULL
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
            
            -- 3. Таблиця Аудиторій (додаємо зараз)
            CREATE TABLE IF NOT EXISTS `ClassRooms` (
                `Id` INT AUTO_INCREMENT PRIMARY KEY,
                `Name` VARCHAR(50) NOT NULL UNIQUE
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
            
            -- 4. Таблиця Предметів (додаємо зараз)
            CREATE TABLE IF NOT EXISTS `Disciplines` (
                `Id` INT AUTO_INCREMENT PRIMARY KEY,
                `Name` VARCHAR(150) NOT NULL UNIQUE,
                `TotalHours` INT NULL
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
            
            -- 5. Таблиця Семестрів (додаємо зараз)
            CREATE TABLE IF NOT EXISTS `Semesters` (
                `Id` INT AUTO_INCREMENT PRIMARY KEY,
                `Name` VARCHAR(100) NOT NULL UNIQUE,
                `StartDate` DATE NOT NULL,
                `EndDate` DATE NOT NULL
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

            -- 6. Головна таблиця Розкладу (додаємо зараз)
            CREATE TABLE IF NOT EXISTS `RealLessons` (
                `Id` INT AUTO_INCREMENT PRIMARY KEY,
                `GroupId` INT NOT NULL,
                `TeacherId` INT NOT NULL,
                `DisciplineId` INT NOT NULL,
                `ClassRoomId` INT NULL,
                `SemesterId` INT NOT NULL,
                `LessonDate` DATE NOT NULL,
                `LessonPosition` INT NOT NULL,
                `WeekDay` INT NOT NULL,
                `WeekProperty` INT NOT NULL,
                `ConferenceLink` VARCHAR(500) NULL, -- Посилання від викладача
                `ResourceLink` VARCHAR(500) NULL,   -- Посилання від викладача
                
                -- Зв'язки (Foreign Keys) з іншими таблицями через чисельні Id
                CONSTRAINT `FK_RealLessons_Group` FOREIGN KEY (`GroupId`) REFERENCES `Groups`(`Id`) ON DELETE CASCADE,
                CONSTRAINT `FK_RealLessons_Teacher` FOREIGN KEY (`TeacherId`) REFERENCES `Teachers`(`Id`) ON DELETE CASCADE,
                CONSTRAINT `FK_RealLessons_Discipline` FOREIGN KEY (`DisciplineId`) REFERENCES `Disciplines`(`Id`) ON DELETE CASCADE,
                CONSTRAINT `FK_RealLessons_ClassRoom` FOREIGN KEY (`ClassRoomId`) REFERENCES `ClassRooms`(`Id`) ON DELETE SET NULL,
                CONSTRAINT `FK_RealLessons_Semester` FOREIGN KEY (`SemesterId`) REFERENCES `Semesters`(`Id`) ON DELETE CASCADE
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

            -- 7. Таблиця Базового розкладу (Шаблон)
            CREATE TABLE IF NOT EXISTS `BaseLessons` (
                `Id` INT AUTO_INCREMENT PRIMARY KEY,
                `GroupId` INT NOT NULL,
                `TeacherId` INT NOT NULL,
                `DisciplineId` INT NOT NULL,
                `ClassRoomId` INT NULL,
                `SemesterId` INT NOT NULL,
                `LessonPosition` INT NOT NULL,
                `WeekDay` INT NOT NULL,
                `WeekProperty` INT NOT NULL,
                
                CONSTRAINT `FK_BaseLessons_Group` FOREIGN KEY (`GroupId`) REFERENCES `Groups`(`Id`) ON DELETE CASCADE,
                CONSTRAINT `FK_BaseLessons_Teacher` FOREIGN KEY (`TeacherId`) REFERENCES `Teachers`(`Id`) ON DELETE CASCADE,
                CONSTRAINT `FK_BaseLessons_Discipline` FOREIGN KEY (`DisciplineId`) REFERENCES `Disciplines`(`Id`) ON DELETE CASCADE,
                CONSTRAINT `FK_BaseLessons_ClassRoom` FOREIGN KEY (`ClassRoomId`) REFERENCES `ClassRooms`(`Id`) ON DELETE SET NULL,
                CONSTRAINT `FK_BaseLessons_Semester` FOREIGN KEY (`SemesterId`) REFERENCES `Semesters`(`Id`) ON DELETE CASCADE
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

        ";

        connection.Execute(createTablesSql);
    }
}