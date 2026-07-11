using System.Data;
using Dapper;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using Schedule.Core.DTOs;
using Schedule.Core.Enums;
using Schedule.Core.Interfaces;
using Schedule.Core.Models;
using Schedule.Core.Services;

namespace Schedule.Infrastructure.Repositories;

public class BaseScheduleReportRepository
    : IBaseScheduleReportRepository
{
    private readonly string _connectionString;

    public BaseScheduleReportRepository(
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

    public async Task<BaseTeacherHoursReportResponse?>
        GetTeacherHoursAsync(
            int semesterId,
            int teacherId,
            int academicHoursPerLesson)
    {
        using var connection = CreateConnection();

        const string sql = """
            SELECT
                tsl.SemesterId,
                semester.Name AS SemesterName,
                semester.StartDate,
                semester.EndDate,
                semester.FirstWeekProperty,
                tsl.TeacherId,
                teacher.Name AS TeacherName,
                tsl.PlannedHours
            FROM `TeacherSemesterLoads` tsl
            INNER JOIN `Semesters` semester
                ON semester.Id = tsl.SemesterId
            INNER JOIN `Teachers` teacher
                ON teacher.Id = tsl.TeacherId
            WHERE
                tsl.SemesterId = @SemesterId
                AND tsl.TeacherId = @TeacherId;

            SELECT
                tdl.Id AS TeacherDisciplineLoadId,
                tdl.DisciplineId,
                discipline.Name AS DisciplineName,
                tdl.PlannedHours
            FROM `TeacherDisciplineLoads` tdl
            INNER JOIN `TeacherSemesterLoads` tsl
                ON tsl.Id = tdl.TeacherSemesterLoadId
            INNER JOIN `Disciplines` discipline
                ON discipline.Id = tdl.DisciplineId
            WHERE
                tsl.SemesterId = @SemesterId
                AND tsl.TeacherId = @TeacherId
            ORDER BY discipline.Name;

            SELECT
                gd.DisciplineId,
                SUM(assignment.AssignedHours)
                    AS AssignedHours
            FROM `TeachingAssignments` assignment
            INNER JOIN `GroupDisciplines` gd
                ON gd.Id = assignment.GroupDisciplineId
            WHERE
                gd.SemesterId = @SemesterId
                AND assignment.TeacherId = @TeacherId
            GROUP BY gd.DisciplineId;

            SELECT
                lesson.DisciplineId,
                lesson.WeekDay,
                lesson.WeekProperty
            FROM `BaseLessons` lesson
            WHERE
                lesson.SemesterId = @SemesterId
                AND lesson.TeacherId = @TeacherId;
            """;

        using var grids =
            await connection.QueryMultipleAsync(
                sql,
                new
                {
                    SemesterId = semesterId,
                    TeacherId = teacherId
                });

        var header =
            await grids.ReadFirstOrDefaultAsync<
                TeacherReportHeader>();

        if (header == null)
        {
            return null;
        }

        var disciplineItems =
            (
                await grids.ReadAsync<
                    BaseTeacherDisciplineHoursItem>()
            ).ToList();

        var assignedHours =
            (
                await grids.ReadAsync<
                    DisciplineAssignedHours>()
            ).ToDictionary(
                item => item.DisciplineId,
                item => item.AssignedHours);

        var baseLessons =
            (
                await grids.ReadAsync<
                    TeacherBaseLessonItem>()
            ).ToList();

        var semester = new Semester
        {
            Id = header.SemesterId,
            Name = header.SemesterName,
            StartDate = header.StartDate,
            EndDate = header.EndDate,
            FirstWeekProperty =
                header.FirstWeekProperty
        };

        foreach (var item in disciplineItems)
        {
            item.AssignedHours =
                assignedHours.GetValueOrDefault(
                    item.DisciplineId);

            item.UnassignedHours = Math.Max(
                0,
                item.PlannedHours -
                item.AssignedHours);

            item.OverAssignedHours = Math.Max(
                0,
                item.AssignedHours -
                item.PlannedHours);

            item.ScheduledLessonCount =
                baseLessons
                    .Where(
                        lesson =>
                            lesson.DisciplineId ==
                            item.DisciplineId)
                    .Sum(
                        lesson =>
                            SemesterCalendar
                                .CountOccurrences(
                                    semester,
                                    lesson.WeekDay,
                                    lesson.WeekProperty));

            item.ScheduledHours =
                item.ScheduledLessonCount *
                academicHoursPerLesson;

            item.RemainingScheduledHours = Math.Max(
                0,
                item.PlannedHours -
                item.ScheduledHours);

            item.ExceededScheduledHours = Math.Max(
                0,
                item.ScheduledHours -
                item.PlannedHours);

            item.IsScheduleExceeded =
                item.ExceededScheduledHours > 0;
        }

        int totalAssignedHours =
            assignedHours.Values.Sum();

        int totalScheduledHours =
            disciplineItems.Sum(
                item => item.ScheduledHours);

        return new BaseTeacherHoursReportResponse
        {
            SemesterId = header.SemesterId,
            SemesterName = header.SemesterName,
            TeacherId = header.TeacherId,
            TeacherName = header.TeacherName,
            AcademicHoursPerLesson =
                academicHoursPerLesson,
            PlannedHours = header.PlannedHours,
            AssignedHours = totalAssignedHours,
            UnassignedHours = Math.Max(
                0,
                header.PlannedHours -
                totalAssignedHours),
            OverAssignedHours = Math.Max(
                0,
                totalAssignedHours -
                header.PlannedHours),
            ScheduledHours = totalScheduledHours,
            RemainingScheduledHours = Math.Max(
                0,
                header.PlannedHours -
                totalScheduledHours),
            ExceededScheduledHours = Math.Max(
                0,
                totalScheduledHours -
                header.PlannedHours),
            IsScheduleExceeded =
                totalScheduledHours >
                header.PlannedHours,
            Disciplines = disciplineItems
        };
    }

    private class TeacherReportHeader
    {
        public int SemesterId { get; set; }
        public string SemesterName { get; set; } =
            string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public WeekProperty FirstWeekProperty
        { get; set; }
        public int TeacherId { get; set; }
        public string TeacherName { get; set; } =
            string.Empty;
        public int PlannedHours { get; set; }
    }

    private class DisciplineAssignedHours
    {
        public int DisciplineId { get; set; }
        public int AssignedHours { get; set; }
    }

    private class TeacherBaseLessonItem
    {
        public int DisciplineId { get; set; }
        public WeekDay WeekDay { get; set; }
        public WeekProperty WeekProperty { get; set; }
    }
}
