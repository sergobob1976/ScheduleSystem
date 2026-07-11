using System.Data;
using Dapper;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using Schedule.Core.DTOs;
using Schedule.Core.Interfaces;

namespace Schedule.Infrastructure.Repositories;

public class RealLessonReportRepository
    : IRealLessonReportRepository
{
    private readonly string _connectionString;

    public RealLessonReportRepository(
        IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString(
                "DefaultConnection")
            ?? throw new InvalidOperationException(
                "Рядок підключення не знайдено.");
    }

    private IDbConnection CreateConnection()
    {
        return new MySqlConnection(_connectionString);
    }

    public async Task<
        IEnumerable<RealLessonDisciplineHoursItem>>
        GetDisciplineHoursAsync(
            int semesterId,
            int groupId,
            DateTime reportDate,
            int academicHoursPerLesson)
    {
        using var connection = CreateConnection();

        const string sql = """
            SELECT
                gd.Id AS GroupDisciplineId,
                d.Id AS DisciplineId,
                d.Name AS DisciplineName,

                (
                    gd.LectureHours +
                    gd.PracticalHours +
                    gd.LaboratoryHours +
                    gd.SeminarHours +
                    gd.OtherHours
                ) AS PlannedHours,

                SUM(
                    CASE
                        WHEN rl.Status = 1
                             AND rl.LessonDate <= @ReportDate
                        THEN 1
                        ELSE 0
                    END
                ) AS CompletedLessonCount,

                SUM(
                    CASE
                        WHEN rl.Status = 0
                             AND rl.LessonDate > @ReportDate
                        THEN 1
                        ELSE 0
                    END
                ) AS PlannedFutureLessonCount,

                SUM(
                    CASE
                        WHEN rl.Status = 0
                             AND rl.LessonDate <= @ReportDate
                        THEN 1
                        ELSE 0
                    END
                ) AS UnconfirmedPastLessonCount,

                SUM(
                    CASE
                        WHEN rl.Status = 2
                             AND rl.LessonDate <= @ReportDate
                        THEN 1
                        ELSE 0
                    END
                ) AS CancelledLessonCount

            FROM `GroupDisciplines` gd

            INNER JOIN `Disciplines` d
                ON d.Id = gd.DisciplineId

            LEFT JOIN `RealLessons` rl
                ON rl.SemesterId = gd.SemesterId
                AND rl.GroupId = gd.GroupId
                AND rl.DisciplineId = gd.DisciplineId

            WHERE
                gd.SemesterId = @SemesterId
                AND gd.GroupId = @GroupId

            GROUP BY
                gd.Id,
                d.Id,
                d.Name,
                gd.LectureHours,
                gd.PracticalHours,
                gd.LaboratoryHours,
                gd.SeminarHours,
                gd.OtherHours

            ORDER BY d.Name;
            """;

        var items =
            (
                await connection.QueryAsync<
                    RealLessonDisciplineHoursItem>(
                    sql,
                    new
                    {
                        SemesterId = semesterId,
                        GroupId = groupId,
                        ReportDate = reportDate.Date
                    })
            ).ToList();

        foreach (var item in items)
        {
            item.CompletedHours =
                item.CompletedLessonCount *
                academicHoursPerLesson;

            item.PlannedFutureHours =
                item.PlannedFutureLessonCount *
                academicHoursPerLesson;

            item.UnconfirmedPastHours =
                item.UnconfirmedPastLessonCount *
                academicHoursPerLesson;

            item.CancelledHours =
                item.CancelledLessonCount *
                academicHoursPerLesson;

            item.RemainingHours = Math.Max(
                0,
                item.PlannedHours -
                item.CompletedHours);

            item.ExceededHours = Math.Max(
                0,
                item.CompletedHours -
                item.PlannedHours);

            item.IsExceeded =
                item.ExceededHours > 0;
        }

        return items;
    }

    public async Task<
        IEnumerable<TeacherDisciplineHoursItem>>
        GetTeacherDisciplineHoursAsync(
            int semesterId,
            int teacherId,
            DateTime reportDate,
            int academicHoursPerLesson)
    {
        using var connection = CreateConnection();

        const string sql = """
            SELECT
                tdl.Id AS TeacherDisciplineLoadId,
                d.Id AS DisciplineId,
                d.Name AS DisciplineName,
                tdl.PlannedHours,

                SUM(
                    CASE
                        WHEN rl.Status = 1
                             AND rl.LessonDate <= @ReportDate
                        THEN 1
                        ELSE 0
                    END
                ) AS CompletedLessonCount,

                SUM(
                    CASE
                        WHEN rl.Status = 0
                             AND rl.LessonDate > @ReportDate
                        THEN 1
                        ELSE 0
                    END
                ) AS PlannedFutureLessonCount,

                SUM(
                    CASE
                        WHEN rl.Status = 0
                             AND rl.LessonDate <= @ReportDate
                        THEN 1
                        ELSE 0
                    END
                ) AS UnconfirmedPastLessonCount,

                SUM(
                    CASE
                        WHEN rl.Status = 2
                             AND rl.LessonDate <= @ReportDate
                        THEN 1
                        ELSE 0
                    END
                ) AS CancelledLessonCount

            FROM `TeacherDisciplineLoads` tdl

            INNER JOIN `TeacherSemesterLoads` tsl
                ON tsl.Id = tdl.TeacherSemesterLoadId

            INNER JOIN `Disciplines` d
                ON d.Id = tdl.DisciplineId

            LEFT JOIN `RealLessons` rl
                ON rl.SemesterId = tsl.SemesterId
                AND rl.TeacherId = tsl.TeacherId
                AND rl.DisciplineId = tdl.DisciplineId

            WHERE
                tsl.SemesterId = @SemesterId
                AND tsl.TeacherId = @TeacherId

            GROUP BY
                tdl.Id,
                d.Id,
                d.Name,
                tdl.PlannedHours

            ORDER BY d.Name;
            """;

        var items =
            (
                await connection.QueryAsync<
                    TeacherDisciplineHoursItem>(
                    sql,
                    new
                    {
                        SemesterId = semesterId,
                        TeacherId = teacherId,
                        ReportDate = reportDate.Date
                    })
            ).ToList();

        foreach (var item in items)
        {
            item.CompletedHours =
                item.CompletedLessonCount *
                academicHoursPerLesson;

            item.PlannedFutureHours =
                item.PlannedFutureLessonCount *
                academicHoursPerLesson;

            item.UnconfirmedPastHours =
                item.UnconfirmedPastLessonCount *
                academicHoursPerLesson;

            item.CancelledHours =
                item.CancelledLessonCount *
                academicHoursPerLesson;

            item.RemainingHours = Math.Max(
                0,
                item.PlannedHours -
                item.CompletedHours);

            item.ExceededHours = Math.Max(
                0,
                item.CompletedHours -
                item.PlannedHours);

            item.IsExceeded =
                item.ExceededHours > 0;
        }

        return items;
    }
}
