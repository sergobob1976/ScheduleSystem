using System.Data;
using Dapper;

namespace Schedule.Infrastructure;

public static class DatabaseSeeder
{
    public static void Seed(IDbConnection connection)
    {
        using IDbTransaction transaction =
            connection.BeginTransaction();

        try
        {
            SeedReferenceData(connection, transaction);
            SeedAcademicData(connection, transaction);
            SeedLoads(connection, transaction);

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private static void SeedReferenceData(
        IDbConnection connection,
        IDbTransaction transaction)
    {
        const string sql = """
            INSERT IGNORE INTO `Semesters`
                (`Name`, `StartDate`, `EndDate`)
            VALUES
                (
                    'Осінній семестр 2026/2027',
                    '2026-09-01',
                    '2026-12-31'
                );


            INSERT IGNORE INTO `Specialties`
                (`Code`, `Name`)
            VALUES
                (
                    '121',
                    'Інженерія програмного забезпечення'
                ),
                (
                    '122',
                    'Комп''ютерні науки'
                );


            INSERT IGNORE INTO `Groups`
                (`Name`)
            VALUES
                ('ІПЗ-21'),
                ('КН-21');


            INSERT IGNORE INTO `GroupSpecialties`
                (`GroupId`, `SpecialtyId`)
            SELECT
                g.`Id`,
                s.`Id`
            FROM `Groups` g
            INNER JOIN `Specialties` s
                ON s.`Code` = '121'
            WHERE g.`Name` = 'ІПЗ-21';


            INSERT IGNORE INTO `GroupSpecialties`
                (`GroupId`, `SpecialtyId`)
            SELECT
                g.`Id`,
                s.`Id`
            FROM `Groups` g
            INNER JOIN `Specialties` s
                ON s.`Code` = '122'
            WHERE g.`Name` = 'КН-21';


            INSERT IGNORE INTO `Teachers`
                (`Name`, `Position`)
            VALUES
                (
                    'Іваненко Олександр Петрович',
                    'Професор'
                ),
                (
                    'Петренко Марія Василівна',
                    'Доцент'
                ),
                (
                    'Коваль Андрій Ігорович',
                    'Старший викладач'
                ),
                (
                    'Мельник Олена Сергіївна',
                    'Доцент'
                ),
                (
                    'Бондар Сергій Миколайович',
                    'Асистент'
                ),
                (
                    'Шевченко Наталія Олегівна',
                    'Старший викладач'
                );


            INSERT IGNORE INTO `ClassRooms`
                (`Name`)
            VALUES
                ('101'),
                ('102'),
                ('201'),
                ('202'),
                ('Комп''ютерна лабораторія 1'),
                ('Комп''ютерна лабораторія 2');
            """;

        connection.Execute(
            sql,
            transaction: transaction);
    }

    private static void SeedAcademicData(
        IDbConnection connection,
        IDbTransaction transaction)
    {
        const string disciplinesSql = """
            INSERT IGNORE INTO `Disciplines`
            (
                `SpecialtyId`,
                `Name`,
                `TotalHours`,
                `LectureHours`,
                `PracticalHours`,
                `LaboratoryHours`,
                `SeminarHours`,
                `OtherHours`
            )
            SELECT
                specialty.`Id`,
                seed.`Name`,
                seed.`TotalHours`,
                seed.`LectureHours`,
                seed.`PracticalHours`,
                seed.`LaboratoryHours`,
                seed.`SeminarHours`,
                seed.`OtherHours`
            FROM `Specialties` specialty
            INNER JOIN
            (
                SELECT
                    '121' AS `SpecialtyCode`,
                    'Об''єктно-орієнтоване програмування'
                        AS `Name`,
                    64 AS `TotalHours`,
                    32 AS `LectureHours`,
                    0 AS `PracticalHours`,
                    32 AS `LaboratoryHours`,
                    0 AS `SeminarHours`,
                    0 AS `OtherHours`

                UNION ALL

                SELECT
                    '121',
                    'Бази даних',
                    64,
                    32,
                    0,
                    32,
                    0,
                    0

                UNION ALL

                SELECT
                    '121',
                    'Вебпрограмування',
                    48,
                    16,
                    0,
                    32,
                    0,
                    0

                UNION ALL

                SELECT
                    '122',
                    'Алгоритми та структури даних',
                    64,
                    32,
                    32,
                    0,
                    0,
                    0

                UNION ALL

                SELECT
                    '122',
                    'Бази даних',
                    64,
                    32,
                    0,
                    32,
                    0,
                    0

                UNION ALL

                SELECT
                    '122',
                    'Комп''ютерні мережі',
                    48,
                    24,
                    0,
                    24,
                    0,
                    0
            ) seed
                ON seed.`SpecialtyCode` =
                   specialty.`Code`;
            """;

        connection.Execute(
            disciplinesSql,
            transaction: transaction);

        const string groupDisciplinesSql = """
            INSERT IGNORE INTO `GroupDisciplines`
            (
                `SemesterId`,
                `GroupId`,
                `DisciplineId`,
                `LectureHours`,
                `PracticalHours`,
                `LaboratoryHours`,
                `SeminarHours`,
                `OtherHours`
            )
            SELECT
                semester.`Id`,
                g.`Id`,
                d.`Id`,
                COALESCE(d.`LectureHours`, 0),
                COALESCE(d.`PracticalHours`, 0),
                COALESCE(d.`LaboratoryHours`, 0),
                COALESCE(d.`SeminarHours`, 0),
                COALESCE(d.`OtherHours`, 0)
            FROM `Groups` g
            INNER JOIN `GroupSpecialties` gs
                ON gs.`GroupId` = g.`Id`
            INNER JOIN `Disciplines` d
                ON d.`SpecialtyId` =
                   gs.`SpecialtyId`
            INNER JOIN `Semesters` semester
                ON semester.`Name` =
                   'Осінній семестр 2026/2027'
            WHERE g.`Name` IN
            (
                'ІПЗ-21',
                'КН-21'
            );
            """;

        connection.Execute(
            groupDisciplinesSql,
            transaction: transaction);

        const string assignmentsSql = """
            INSERT IGNORE INTO `TeachingAssignments`
            (
                `GroupDisciplineId`,
                `TeacherId`,
                `LessonType`,
                `AssignedHours`
            )
            SELECT
                gd.`Id`,
                teacher.`Id`,
                assignment.`LessonType`,
                assignment.`AssignedHours`
            FROM `GroupDisciplines` gd
            INNER JOIN `Groups` g
                ON g.`Id` = gd.`GroupId`
            INNER JOIN `Disciplines` d
                ON d.`Id` = gd.`DisciplineId`
            INNER JOIN `Semesters` semester
                ON semester.`Id` =
                   gd.`SemesterId`
            INNER JOIN
            (
                SELECT
                    'ІПЗ-21' AS `GroupName`,
                    'Об''єктно-орієнтоване програмування'
                        AS `DisciplineName`,
                    'Іваненко Олександр Петрович'
                        AS `TeacherName`,
                    1 AS `LessonType`,
                    32 AS `AssignedHours`

                UNION ALL

                SELECT
                    'ІПЗ-21',
                    'Об''єктно-орієнтоване програмування',
                    'Коваль Андрій Ігорович',
                    3,
                    32

                UNION ALL

                SELECT
                    'ІПЗ-21',
                    'Бази даних',
                    'Петренко Марія Василівна',
                    1,
                    32

                UNION ALL

                SELECT
                    'ІПЗ-21',
                    'Бази даних',
                    'Бондар Сергій Миколайович',
                    3,
                    32

                UNION ALL

                SELECT
                    'ІПЗ-21',
                    'Вебпрограмування',
                    'Шевченко Наталія Олегівна',
                    1,
                    16

                UNION ALL

                SELECT
                    'ІПЗ-21',
                    'Вебпрограмування',
                    'Шевченко Наталія Олегівна',
                    3,
                    32

                UNION ALL

                SELECT
                    'КН-21',
                    'Алгоритми та структури даних',
                    'Іваненко Олександр Петрович',
                    1,
                    32

                UNION ALL

                SELECT
                    'КН-21',
                    'Алгоритми та структури даних',
                    'Мельник Олена Сергіївна',
                    2,
                    32

                UNION ALL

                SELECT
                    'КН-21',
                    'Бази даних',
                    'Петренко Марія Василівна',
                    1,
                    32

                UNION ALL

                SELECT
                    'КН-21',
                    'Бази даних',
                    'Бондар Сергій Миколайович',
                    3,
                    32

                UNION ALL

                SELECT
                    'КН-21',
                    'Комп''ютерні мережі',
                    'Мельник Олена Сергіївна',
                    1,
                    24

                UNION ALL

                SELECT
                    'КН-21',
                    'Комп''ютерні мережі',
                    'Коваль Андрій Ігорович',
                    3,
                    24
            ) assignment
                ON assignment.`GroupName` =
                   g.`Name`
                AND assignment.`DisciplineName` =
                    d.`Name`
            INNER JOIN `Teachers` teacher
                ON teacher.`Name` =
                   assignment.`TeacherName`
            WHERE semester.`Name` =
                  'Осінній семестр 2026/2027';
            """;

        connection.Execute(
            assignmentsSql,
            transaction: transaction);
    }

    private static void SeedLoads(
        IDbConnection connection,
        IDbTransaction transaction)
    {
        const string semesterLoadsSql = """
            INSERT IGNORE INTO `TeacherSemesterLoads`
            (
                `TeacherId`,
                `SemesterId`,
                `PlannedHours`
            )
            SELECT
                teacher.`Id`,
                semester.`Id`,
                loadData.`PlannedHours`
            FROM `Teachers` teacher
            INNER JOIN
            (
                SELECT
                    'Іваненко Олександр Петрович'
                        AS `TeacherName`,
                    64 AS `PlannedHours`

                UNION ALL

                SELECT
                    'Петренко Марія Василівна',
                    64

                UNION ALL

                SELECT
                    'Коваль Андрій Ігорович',
                    56

                UNION ALL

                SELECT
                    'Мельник Олена Сергіївна',
                    56

                UNION ALL

                SELECT
                    'Бондар Сергій Миколайович',
                    64

                UNION ALL

                SELECT
                    'Шевченко Наталія Олегівна',
                    48
            ) loadData
                ON loadData.`TeacherName` =
                   teacher.`Name`
            INNER JOIN `Semesters` semester
                ON semester.`Name` =
                   'Осінній семестр 2026/2027';
            """;

        connection.Execute(
            semesterLoadsSql,
            transaction: transaction);

        const string disciplineLoadsSql = """
            INSERT IGNORE INTO `TeacherDisciplineLoads`
            (
                `TeacherSemesterLoadId`,
                `DisciplineId`,
                `PlannedHours`
            )
            SELECT
                tsl.`Id`,
                assignmentData.`DisciplineId`,
                SUM(assignmentData.`AssignedHours`)
            FROM `TeacherSemesterLoads` tsl
            INNER JOIN `Semesters` semester
                ON semester.`Id` =
                   tsl.`SemesterId`
            INNER JOIN
            (
                SELECT
                    ta.`TeacherId`,
                    gd.`DisciplineId`,
                    ta.`AssignedHours`
                FROM `TeachingAssignments` ta
                INNER JOIN `GroupDisciplines` gd
                    ON gd.`Id` =
                       ta.`GroupDisciplineId`
                INNER JOIN `Semesters` semester2
                    ON semester2.`Id` =
                       gd.`SemesterId`
                WHERE semester2.`Name` =
                      'Осінній семестр 2026/2027'
            ) assignmentData
                ON assignmentData.`TeacherId` =
                   tsl.`TeacherId`
            WHERE semester.`Name` =
                  'Осінній семестр 2026/2027'
            GROUP BY
                tsl.`Id`,
                assignmentData.`DisciplineId`;
            """;

        connection.Execute(
            disciplineLoadsSql,
            transaction: transaction);
    }
}