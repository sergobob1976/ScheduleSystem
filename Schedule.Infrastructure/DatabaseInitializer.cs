using System.Data;
using Dapper;
using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace Schedule.Infrastructure;

public class DatabaseInitializer
{
    private readonly string _connectionString;

    public DatabaseInitializer(
        IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString(
                "DefaultConnection")
            ?? throw new InvalidOperationException(
                "Рядок підключення не знайдено.");
    }

    public void Initialize()
    {
        using IDbConnection connection =
            new MySqlConnection(_connectionString);

        connection.Open();

        CreateTables(connection);
        UpdateExistingTables(connection);
        DatabaseSeeder.Seed(connection);
    }

    private static void CreateTables(
        IDbConnection connection)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS `Groups`
            (
                `Id` INT AUTO_INCREMENT PRIMARY KEY,
                `Name` VARCHAR(50) NOT NULL UNIQUE
            )
            ENGINE=InnoDB
            DEFAULT CHARSET=utf8mb4
            COLLATE=utf8mb4_unicode_ci;


            CREATE TABLE IF NOT EXISTS `Specialties`
            (
                `Id` INT AUTO_INCREMENT PRIMARY KEY,
                `Code` VARCHAR(20) NOT NULL,
                `Name` VARCHAR(200) NOT NULL,

                UNIQUE KEY `UX_Specialties_Code`
                    (`Code`)
            )
            ENGINE=InnoDB
            DEFAULT CHARSET=utf8mb4
            COLLATE=utf8mb4_unicode_ci;


            CREATE TABLE IF NOT EXISTS `GroupSpecialties`
            (
                `Id` INT AUTO_INCREMENT PRIMARY KEY,
                `GroupId` INT NOT NULL,
                `SpecialtyId` INT NOT NULL,

                CONSTRAINT `FK_GroupSpecialties_Group`
                    FOREIGN KEY (`GroupId`)
                    REFERENCES `Groups` (`Id`)
                    ON DELETE CASCADE,

                CONSTRAINT `FK_GroupSpecialties_Specialty`
                    FOREIGN KEY (`SpecialtyId`)
                    REFERENCES `Specialties` (`Id`)
                    ON DELETE CASCADE,

                UNIQUE KEY
                    `UX_GroupSpecialties_Group_Specialty`
                    (`GroupId`, `SpecialtyId`)
            )
            ENGINE=InnoDB
            DEFAULT CHARSET=utf8mb4
            COLLATE=utf8mb4_unicode_ci;


            CREATE TABLE IF NOT EXISTS `Teachers`
            (
                `Id` INT AUTO_INCREMENT PRIMARY KEY,
                `Name` VARCHAR(100) NOT NULL UNIQUE,
                `Position` VARCHAR(100) NULL
            )
            ENGINE=InnoDB
            DEFAULT CHARSET=utf8mb4
            COLLATE=utf8mb4_unicode_ci;


            CREATE TABLE IF NOT EXISTS `ClassRooms`
            (
                `Id` INT AUTO_INCREMENT PRIMARY KEY,
                `Name` VARCHAR(50) NOT NULL UNIQUE
            )
            ENGINE=InnoDB
            DEFAULT CHARSET=utf8mb4
            COLLATE=utf8mb4_unicode_ci;


            CREATE TABLE IF NOT EXISTS `Semesters`
            (
                `Id` INT AUTO_INCREMENT PRIMARY KEY,
                `Name` VARCHAR(100) NOT NULL UNIQUE,
                `StartDate` DATE NOT NULL,
                `EndDate` DATE NOT NULL,
                `FirstWeekProperty` INT NOT NULL DEFAULT 1,

                CHECK (`EndDate` >= `StartDate`),
                CHECK (`FirstWeekProperty` IN (1, 2))
            )
            ENGINE=InnoDB
            DEFAULT CHARSET=utf8mb4
            COLLATE=utf8mb4_unicode_ci;


            CREATE TABLE IF NOT EXISTS `Disciplines`
            (
                `Id` INT AUTO_INCREMENT PRIMARY KEY,
                `SpecialtyId` INT NOT NULL,
                `Name` VARCHAR(150) NOT NULL,

                `TotalHours` INT NULL,
                `LectureHours` INT NULL,
                `PracticalHours` INT NULL,
                `LaboratoryHours` INT NULL,
                `SeminarHours` INT NULL,
                `OtherHours` INT NULL,

                CONSTRAINT `FK_Disciplines_Specialty`
                    FOREIGN KEY (`SpecialtyId`)
                    REFERENCES `Specialties` (`Id`)
                    ON DELETE CASCADE,

                UNIQUE KEY
                    `UX_Disciplines_Specialty_Name`
                    (`SpecialtyId`, `Name`)
            )
            ENGINE=InnoDB
            DEFAULT CHARSET=utf8mb4
            COLLATE=utf8mb4_unicode_ci;


            /*
             * Старий зв'язок тимчасово залишається
             * для сумісності з наявним кодом.
             */
            CREATE TABLE IF NOT EXISTS `TeacherDisciplines`
            (
                `Id` INT AUTO_INCREMENT PRIMARY KEY,
                `TeacherId` INT NOT NULL,
                `DisciplineId` INT NOT NULL,

                CONSTRAINT `FK_TeacherDisciplines_Teacher`
                    FOREIGN KEY (`TeacherId`)
                    REFERENCES `Teachers` (`Id`)
                    ON DELETE CASCADE,

                CONSTRAINT `FK_TeacherDisciplines_Discipline`
                    FOREIGN KEY (`DisciplineId`)
                    REFERENCES `Disciplines` (`Id`)
                    ON DELETE CASCADE,

                UNIQUE KEY
                    `UX_TeacherDisciplines_Teacher_Discipline`
                    (`TeacherId`, `DisciplineId`)
            )
            ENGINE=InnoDB
            DEFAULT CHARSET=utf8mb4
            COLLATE=utf8mb4_unicode_ci;


            CREATE TABLE IF NOT EXISTS `TeacherSemesterLoads`
            (
                `Id` INT AUTO_INCREMENT PRIMARY KEY,
                `TeacherId` INT NOT NULL,
                `SemesterId` INT NOT NULL,
                `PlannedHours` INT NOT NULL,

                CONSTRAINT `FK_TeacherSemesterLoads_Teacher`
                    FOREIGN KEY (`TeacherId`)
                    REFERENCES `Teachers` (`Id`)
                    ON DELETE CASCADE,

                CONSTRAINT `FK_TeacherSemesterLoads_Semester`
                    FOREIGN KEY (`SemesterId`)
                    REFERENCES `Semesters` (`Id`)
                    ON DELETE CASCADE,

                UNIQUE KEY
                    `UX_TeacherSemesterLoads_Teacher_Semester`
                    (`TeacherId`, `SemesterId`),

                INDEX `IX_TeacherSemesterLoads_Semester`
                    (`SemesterId`),

                CHECK (`PlannedHours` >= 0)
            )
            ENGINE=InnoDB
            DEFAULT CHARSET=utf8mb4
            COLLATE=utf8mb4_unicode_ci;


            CREATE TABLE IF NOT EXISTS `TeacherDisciplineLoads`
            (
                `Id` INT AUTO_INCREMENT PRIMARY KEY,
                `TeacherSemesterLoadId` INT NOT NULL,
                `DisciplineId` INT NOT NULL,
                `PlannedHours` INT NOT NULL,

                CONSTRAINT
                    `FK_TeacherDisciplineLoads_TeacherSemesterLoad`
                    FOREIGN KEY (`TeacherSemesterLoadId`)
                    REFERENCES `TeacherSemesterLoads` (`Id`)
                    ON DELETE CASCADE,

                CONSTRAINT
                    `FK_TeacherDisciplineLoads_Discipline`
                    FOREIGN KEY (`DisciplineId`)
                    REFERENCES `Disciplines` (`Id`)
                    ON DELETE CASCADE,

                UNIQUE KEY
                    `UX_TeacherDisciplineLoads_Load_Discipline`
                    (`TeacherSemesterLoadId`, `DisciplineId`),

                INDEX `IX_TeacherDisciplineLoads_Discipline`
                    (`DisciplineId`),

                CHECK (`PlannedHours` >= 0)
            )
            ENGINE=InnoDB
            DEFAULT CHARSET=utf8mb4
            COLLATE=utf8mb4_unicode_ci;


            CREATE TABLE IF NOT EXISTS `GroupDisciplines`
            (
                `Id` INT AUTO_INCREMENT PRIMARY KEY,
                `SemesterId` INT NOT NULL,
                `GroupId` INT NOT NULL,
                `DisciplineId` INT NOT NULL,

                `LectureHours` INT NOT NULL DEFAULT 0,
                `PracticalHours` INT NOT NULL DEFAULT 0,
                `LaboratoryHours` INT NOT NULL DEFAULT 0,
                `SeminarHours` INT NOT NULL DEFAULT 0,
                `OtherHours` INT NOT NULL DEFAULT 0,

                CONSTRAINT `FK_GroupDisciplines_Semester`
                    FOREIGN KEY (`SemesterId`)
                    REFERENCES `Semesters` (`Id`)
                    ON DELETE CASCADE,

                CONSTRAINT `FK_GroupDisciplines_Group`
                    FOREIGN KEY (`GroupId`)
                    REFERENCES `Groups` (`Id`)
                    ON DELETE CASCADE,

                CONSTRAINT `FK_GroupDisciplines_Discipline`
                    FOREIGN KEY (`DisciplineId`)
                    REFERENCES `Disciplines` (`Id`)
                    ON DELETE CASCADE,

                UNIQUE KEY
                    `UX_GroupDisciplines_Semester_Group_Discipline`
                    (`SemesterId`, `GroupId`, `DisciplineId`),

                INDEX `IX_GroupDisciplines_Group_Semester`
                    (`GroupId`, `SemesterId`),

                INDEX `IX_GroupDisciplines_Discipline_Semester`
                    (`DisciplineId`, `SemesterId`),

                CHECK (`LectureHours` >= 0),
                CHECK (`PracticalHours` >= 0),
                CHECK (`LaboratoryHours` >= 0),
                CHECK (`SeminarHours` >= 0),
                CHECK (`OtherHours` >= 0)
            )
            ENGINE=InnoDB
            DEFAULT CHARSET=utf8mb4
            COLLATE=utf8mb4_unicode_ci;


            CREATE TABLE IF NOT EXISTS `TeachingAssignments`
            (
                `Id` INT AUTO_INCREMENT PRIMARY KEY,
                `GroupDisciplineId` INT NOT NULL,
                `TeacherId` INT NOT NULL,
                `LessonType` INT NOT NULL,
                `AssignedHours` INT NOT NULL,

                CONSTRAINT
                    `FK_TeachingAssignments_GroupDiscipline`
                    FOREIGN KEY (`GroupDisciplineId`)
                    REFERENCES `GroupDisciplines` (`Id`)
                    ON DELETE CASCADE,

                CONSTRAINT `FK_TeachingAssignments_Teacher`
                    FOREIGN KEY (`TeacherId`)
                    REFERENCES `Teachers` (`Id`)
                    ON DELETE CASCADE,

                UNIQUE KEY
                    `UX_TeachingAssignments_GroupDiscipline_Teacher_Type`
                    (
                        `GroupDisciplineId`,
                        `TeacherId`,
                        `LessonType`
                    ),

                INDEX `IX_TeachingAssignments_Teacher`
                    (`TeacherId`),

                INDEX `IX_TeachingAssignments_GroupDiscipline`
                    (`GroupDisciplineId`),

                CHECK (`AssignedHours` >= 0)
            )
            ENGINE=InnoDB
            DEFAULT CHARSET=utf8mb4
            COLLATE=utf8mb4_unicode_ci;


            CREATE TABLE IF NOT EXISTS `RealLessons`
            (
                `Id` INT AUTO_INCREMENT PRIMARY KEY,

                `TeachingAssignmentId` INT NULL,

                `GroupId` INT NOT NULL,
                `TeacherId` INT NOT NULL,
                `DisciplineId` INT NOT NULL,
                `ClassRoomId` INT NULL,
                `SemesterId` INT NOT NULL,

                `LessonDate` DATE NOT NULL,
                `LessonPosition` INT NOT NULL,
                `WeekDay` INT NOT NULL,
                `WeekProperty` INT NOT NULL,
                `LessonType` INT NOT NULL,
                `Status` INT NOT NULL DEFAULT 0,

                `ConferenceLink` VARCHAR(500) NULL,
                `ResourceLink` VARCHAR(500) NULL,

                CONSTRAINT
                    `FK_RealLessons_TeachingAssignment`
                    FOREIGN KEY (`TeachingAssignmentId`)
                    REFERENCES `TeachingAssignments` (`Id`)
                    ON DELETE SET NULL,

                CONSTRAINT `FK_RealLessons_Group`
                    FOREIGN KEY (`GroupId`)
                    REFERENCES `Groups` (`Id`)
                    ON DELETE CASCADE,

                CONSTRAINT `FK_RealLessons_Teacher`
                    FOREIGN KEY (`TeacherId`)
                    REFERENCES `Teachers` (`Id`)
                    ON DELETE CASCADE,

                CONSTRAINT `FK_RealLessons_Discipline`
                    FOREIGN KEY (`DisciplineId`)
                    REFERENCES `Disciplines` (`Id`)
                    ON DELETE CASCADE,

                CONSTRAINT `FK_RealLessons_ClassRoom`
                    FOREIGN KEY (`ClassRoomId`)
                    REFERENCES `ClassRooms` (`Id`)
                    ON DELETE SET NULL,

                CONSTRAINT `FK_RealLessons_Semester`
                    FOREIGN KEY (`SemesterId`)
                    REFERENCES `Semesters` (`Id`)
                    ON DELETE CASCADE,

                CHECK (`LessonPosition` BETWEEN 1 AND 8),
                CHECK (`Status` IN (0, 1, 2)),

                INDEX `IX_RealLessons_TeachingAssignment`
                    (`TeachingAssignmentId`),

                INDEX `IX_RealLessons_Group_Date`
                    (`GroupId`, `LessonDate`),

                INDEX `IX_RealLessons_Teacher_Date`
                    (`TeacherId`, `LessonDate`)
            )
            ENGINE=InnoDB
            DEFAULT CHARSET=utf8mb4
            COLLATE=utf8mb4_unicode_ci;


            CREATE TABLE IF NOT EXISTS `RealLessonWeekTransfers`
            (
                `Id` INT AUTO_INCREMENT PRIMARY KEY,
                `SemesterId` INT NOT NULL,
                `WeekStartDate` DATE NOT NULL,
                `WeekEndDate` DATE NOT NULL,
                `WeekProperty` INT NOT NULL,
                `CreatedAt` DATETIME NOT NULL
                    DEFAULT CURRENT_TIMESTAMP,

                CONSTRAINT
                    `FK_RealLessonWeekTransfers_Semester`
                    FOREIGN KEY (`SemesterId`)
                    REFERENCES `Semesters` (`Id`)
                    ON DELETE CASCADE,

                UNIQUE KEY
                    `UX_RealLessonWeekTransfers_Semester_Week`
                    (`SemesterId`, `WeekStartDate`),

                CHECK (`WeekEndDate` >= `WeekStartDate`),
                CHECK (`WeekProperty` IN (1, 2))
            )
            ENGINE=InnoDB
            DEFAULT CHARSET=utf8mb4
            COLLATE=utf8mb4_unicode_ci;


            CREATE TABLE IF NOT EXISTS `BaseLessons`
            (
                `Id` INT AUTO_INCREMENT PRIMARY KEY,

                `TeachingAssignmentId` INT NULL,

                `GroupId` INT NOT NULL,
                `TeacherId` INT NOT NULL,
                `DisciplineId` INT NOT NULL,
                `ClassRoomId` INT NULL,
                `SemesterId` INT NOT NULL,

                `LessonPosition` INT NOT NULL,
                `WeekDay` INT NOT NULL,
                `WeekProperty` INT NOT NULL,
                `LessonType` INT NOT NULL,

                CONSTRAINT
                    `FK_BaseLessons_TeachingAssignment`
                    FOREIGN KEY (`TeachingAssignmentId`)
                    REFERENCES `TeachingAssignments` (`Id`)
                    ON DELETE SET NULL,

                CONSTRAINT `FK_BaseLessons_Group`
                    FOREIGN KEY (`GroupId`)
                    REFERENCES `Groups` (`Id`)
                    ON DELETE CASCADE,

                CONSTRAINT `FK_BaseLessons_Teacher`
                    FOREIGN KEY (`TeacherId`)
                    REFERENCES `Teachers` (`Id`)
                    ON DELETE CASCADE,

                CONSTRAINT `FK_BaseLessons_Discipline`
                    FOREIGN KEY (`DisciplineId`)
                    REFERENCES `Disciplines` (`Id`)
                    ON DELETE CASCADE,

                CONSTRAINT `FK_BaseLessons_ClassRoom`
                    FOREIGN KEY (`ClassRoomId`)
                    REFERENCES `ClassRooms` (`Id`)
                    ON DELETE SET NULL,

                CONSTRAINT `FK_BaseLessons_Semester`
                    FOREIGN KEY (`SemesterId`)
                    REFERENCES `Semesters` (`Id`)
                    ON DELETE CASCADE,

                CHECK (`LessonPosition` BETWEEN 1 AND 8),

                INDEX `IX_BaseLessons_TeachingAssignment`
                    (`TeachingAssignmentId`),

                INDEX `IX_BaseLessons_Group_Semester`
                    (`GroupId`, `SemesterId`),

                INDEX `IX_BaseLessons_Teacher_Semester`
                    (`TeacherId`, `SemesterId`)
            )
            ENGINE=InnoDB
            DEFAULT CHARSET=utf8mb4
            COLLATE=utf8mb4_unicode_ci;
            """;

        connection.Execute(sql);
    }

    private static void UpdateExistingTables(
        IDbConnection connection)
    {
        EnsureColumn(
            connection,
            "Semesters",
            "FirstWeekProperty",
            """
            ALTER TABLE `Semesters`
            ADD COLUMN `FirstWeekProperty` INT NOT NULL DEFAULT 1
            AFTER `EndDate`;
            """);

        EnsureColumn(
            connection,
            "RealLessons",
            "Status",
            """
            ALTER TABLE `RealLessons`
            ADD COLUMN `Status` INT NOT NULL DEFAULT 0
            AFTER `LessonType`;
            """);

        EnsureColumn(
            connection,
            "RealLessons",
            "TeachingAssignmentId",
            """
            ALTER TABLE `RealLessons`
            ADD COLUMN `TeachingAssignmentId` INT NULL
            AFTER `Id`;
            """);

        EnsureColumn(
            connection,
            "BaseLessons",
            "TeachingAssignmentId",
            """
            ALTER TABLE `BaseLessons`
            ADD COLUMN `TeachingAssignmentId` INT NULL
            AFTER `Id`;
            """);

        EnsureIndex(
            connection,
            "RealLessons",
            "IX_RealLessons_TeachingAssignment",
            """
            CREATE INDEX
                `IX_RealLessons_TeachingAssignment`
            ON `RealLessons` (`TeachingAssignmentId`);
            """);

        EnsureIndex(
            connection,
            "BaseLessons",
            "IX_BaseLessons_TeachingAssignment",
            """
            CREATE INDEX
                `IX_BaseLessons_TeachingAssignment`
            ON `BaseLessons` (`TeachingAssignmentId`);
            """);

        EnsureForeignKey(
            connection,
            "RealLessons",
            "FK_RealLessons_TeachingAssignment",
            """
            ALTER TABLE `RealLessons`
            ADD CONSTRAINT
                `FK_RealLessons_TeachingAssignment`
            FOREIGN KEY (`TeachingAssignmentId`)
            REFERENCES `TeachingAssignments` (`Id`)
            ON DELETE SET NULL;
            """);

        EnsureForeignKey(
            connection,
            "BaseLessons",
            "FK_BaseLessons_TeachingAssignment",
            """
            ALTER TABLE `BaseLessons`
            ADD CONSTRAINT
                `FK_BaseLessons_TeachingAssignment`
            FOREIGN KEY (`TeachingAssignmentId`)
            REFERENCES `TeachingAssignments` (`Id`)
            ON DELETE SET NULL;
            """);
    }

    private static void EnsureColumn(
        IDbConnection connection,
        string tableName,
        string columnName,
        string alterSql)
    {
        const string checkSql = """
            SELECT COUNT(*)
            FROM information_schema.COLUMNS
            WHERE
                TABLE_SCHEMA = DATABASE()
                AND TABLE_NAME = @TableName
                AND COLUMN_NAME = @ColumnName;
            """;

        int count =
            connection.ExecuteScalar<int>(
                checkSql,
                new
                {
                    TableName = tableName,
                    ColumnName = columnName
                });

        if (count == 0)
        {
            connection.Execute(alterSql);
        }
    }

    private static void EnsureIndex(
        IDbConnection connection,
        string tableName,
        string indexName,
        string createSql)
    {
        const string checkSql = """
            SELECT COUNT(*)
            FROM information_schema.STATISTICS
            WHERE
                TABLE_SCHEMA = DATABASE()
                AND TABLE_NAME = @TableName
                AND INDEX_NAME = @IndexName;
            """;

        int count =
            connection.ExecuteScalar<int>(
                checkSql,
                new
                {
                    TableName = tableName,
                    IndexName = indexName
                });

        if (count == 0)
        {
            connection.Execute(createSql);
        }
    }

    private static void EnsureForeignKey(
        IDbConnection connection,
        string tableName,
        string constraintName,
        string alterSql)
    {
        const string checkSql = """
            SELECT COUNT(*)
            FROM information_schema.TABLE_CONSTRAINTS
            WHERE
                CONSTRAINT_SCHEMA = DATABASE()
                AND TABLE_NAME = @TableName
                AND CONSTRAINT_NAME = @ConstraintName
                AND CONSTRAINT_TYPE = 'FOREIGN KEY';
            """;

        int count =
            connection.ExecuteScalar<int>(
                checkSql,
                new
                {
                    TableName = tableName,
                    ConstraintName = constraintName
                });

        if (count == 0)
        {
            connection.Execute(alterSql);
        }
    }
}
