using Microsoft.AspNetCore.Mvc;
using Schedule.Core.DTOs;
using Schedule.Core.Enums;
using Schedule.Core.Interfaces;
using Schedule.Core.Models;
using Schedule.Core.Services;

namespace Schedule.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BaseScheduleBuilderController
    : ControllerBase
{
    private const int AcademicHoursPerLesson = 2;

    private readonly ISemesterRepository
        _semesterRepository;

    private readonly IGroupRepository
        _groupRepository;

    private readonly IGroupDisciplineRepository
        _groupDisciplineRepository;

    private readonly ITeachingAssignmentRepository
        _teachingAssignmentRepository;

    private readonly IBaseLessonRepository
        _baseLessonRepository;

    public BaseScheduleBuilderController(
        ISemesterRepository semesterRepository,
        IGroupRepository groupRepository,
        IGroupDisciplineRepository
            groupDisciplineRepository,
        ITeachingAssignmentRepository
            teachingAssignmentRepository,
        IBaseLessonRepository baseLessonRepository)
    {
        _semesterRepository =
            semesterRepository;

        _groupRepository =
            groupRepository;

        _groupDisciplineRepository =
            groupDisciplineRepository;

        _teachingAssignmentRepository =
            teachingAssignmentRepository;

        _baseLessonRepository =
            baseLessonRepository;
    }

    [HttpGet(
        "semester/{semesterId:int}/group/{groupId:int}")]
    public async Task<
        ActionResult<BaseScheduleBuilderResponse>>
        GetBuilderData(
            int semesterId,
            int groupId)
    {
        if (semesterId <= 0)
        {
            return BadRequest(new
            {
                Message =
                    "Потрібно обрати семестр."
            });
        }

        if (groupId <= 0)
        {
            return BadRequest(new
            {
                Message =
                    "Потрібно обрати групу."
            });
        }

        var semester =
            await _semesterRepository.GetByIdAsync(
                semesterId);

        if (semester == null)
        {
            return NotFound(new
            {
                Message =
                    $"Семестр з ID {semesterId} " +
                    "не знайдено."
            });
        }

        if (semester.EndDate.Date <
            semester.StartDate.Date)
        {
            return BadRequest(new
            {
                Message =
                    "Дата завершення семестру не може " +
                    "бути раніше дати початку."
            });
        }

        var group =
            await _groupRepository.GetByIdAsync(
                groupId);

        if (group == null)
        {
            return NotFound(new
            {
                Message =
                    $"Групу з ID {groupId} не знайдено."
            });
        }

        var groupDisciplines =
            (
                await _groupDisciplineRepository
                    .GetBySemesterAndGroupAsync(
                        semesterId,
                        groupId)
            ).ToList();

        var groupBaseLessons =
            (
                await _baseLessonRepository
                    .GetByGroupIdAsync(groupId)
            )
            .Where(
                lesson =>
                    lesson.SemesterId ==
                    semesterId)
            .ToList();

        var response =
            new BaseScheduleBuilderResponse
            {
                SemesterId =
                    semester.Id,

                SemesterName =
                    semester.Name,

                SemesterStartDate =
                    semester.StartDate,

                SemesterEndDate =
                    semester.EndDate,

                FirstWeekProperty =
                    semester.FirstWeekProperty,

                GroupId =
                    group.Id,

                GroupName =
                    group.Name,

                AcademicHoursPerLesson =
                    AcademicHoursPerLesson
            };

        foreach (var groupDiscipline
                 in groupDisciplines)
        {
            var assignments =
                (
                    await _teachingAssignmentRepository
                        .GetByGroupDisciplineIdAsync(
                            groupDiscipline.Id)
                ).ToList();

            var disciplineItem =
                new BaseScheduleDisciplineItem
                {
                    GroupDisciplineId =
                        groupDiscipline.Id,

                    DisciplineId =
                        groupDiscipline.DisciplineId,

                    DisciplineName =
                        groupDiscipline.Discipline?.Name
                        ?? string.Empty,

                    TotalPlannedHours =
                        groupDiscipline.TotalHours,

                    LectureHours =
                        groupDiscipline.LectureHours,

                    PracticalHours =
                        groupDiscipline.PracticalHours,

                    LaboratoryHours =
                        groupDiscipline
                            .LaboratoryHours,

                    SeminarHours =
                        groupDiscipline.SeminarHours,

                    OtherHours =
                        groupDiscipline.OtherHours
                };

            foreach (var assignment
                     in assignments)
            {
                var assignmentBaseLessons =
                    groupBaseLessons
                        .Where(
                            lesson =>
                                lesson
                                    .TeachingAssignmentId ==
                                assignment.Id)
                        .ToList();

                int scheduledLessonCount =
                    CalculateScheduledLessonCount(
                        assignmentBaseLessons,
                        semester);

                int scheduledHours =
                    scheduledLessonCount *
                    AcademicHoursPerLesson;

                int remainingHours =
                    Math.Max(
                        0,
                        assignment.AssignedHours -
                        scheduledHours);

                int exceededHours =
                    Math.Max(
                        0,
                        scheduledHours -
                        assignment.AssignedHours);

                disciplineItem
                    .TeachingAssignments
                    .Add(
                        new BaseScheduleAssignmentItem
                        {
                            TeachingAssignmentId =
                                assignment.Id,

                            TeacherId =
                                assignment.TeacherId,

                            TeacherName =
                                assignment.Teacher?.Name
                                ?? string.Empty,

                            LessonType =
                                assignment.LessonType,

                            AssignedHours =
                                assignment.AssignedHours,

                            ScheduledLessonCount =
                                scheduledLessonCount,

                            ScheduledHours =
                                scheduledHours,

                            RemainingHours =
                                remainingHours,

                            ExceededHours =
                                exceededHours,

                            IsExceeded =
                                exceededHours > 0
                        });
            }

            disciplineItem.TotalAssignedHours =
                disciplineItem.TeachingAssignments
                    .Sum(item => item.AssignedHours);

            disciplineItem.UnassignedHours =
                Math.Max(
                    0,
                    disciplineItem.TotalPlannedHours -
                    disciplineItem.TotalAssignedHours);

            disciplineItem.OverAssignedHours =
                Math.Max(
                    0,
                    disciplineItem.TotalAssignedHours -
                    disciplineItem.TotalPlannedHours);

            disciplineItem.ScheduledLessonCount =
                disciplineItem.TeachingAssignments
                    .Sum(
                        item =>
                            item.ScheduledLessonCount);

            disciplineItem.ScheduledHours =
                disciplineItem.TeachingAssignments
                    .Sum(item => item.ScheduledHours);

            disciplineItem.RemainingScheduledHours =
                Math.Max(
                    0,
                    disciplineItem.TotalPlannedHours -
                    disciplineItem.ScheduledHours);

            disciplineItem.ExceededScheduledHours =
                Math.Max(
                    0,
                    disciplineItem.ScheduledHours -
                    disciplineItem.TotalPlannedHours);

            disciplineItem.IsScheduleExceeded =
                disciplineItem.ExceededScheduledHours > 0;

            response.Disciplines.Add(
                disciplineItem);
        }

        return Ok(response);
    }

    [HttpGet(
        "semester/{semesterId:int}/group/{groupId:int}/projection")]
    public async Task<
        ActionResult<BaseScheduleProjectionResponse>>
        GetProjection(
            int semesterId,
            int groupId)
    {
        var semester =
            await _semesterRepository.GetByIdAsync(
                semesterId);

        if (semester == null)
        {
            return NotFound(new
            {
                Message =
                    $"Семестр з ID {semesterId} " +
                    "не знайдено."
            });
        }

        var group =
            await _groupRepository.GetByIdAsync(
                groupId);

        if (group == null)
        {
            return NotFound(new
            {
                Message =
                    $"Групу з ID {groupId} не знайдено."
            });
        }

        var baseLessons =
            (
                await _baseLessonRepository
                    .GetByGroupIdAsync(groupId)
            )
            .Where(
                lesson =>
                    lesson.SemesterId == semesterId)
            .ToList();

        var response =
            new BaseScheduleProjectionResponse
            {
                SemesterId = semester.Id,
                SemesterName = semester.Name,
                GroupId = group.Id,
                GroupName = group.Name,
                FirstWeekProperty =
                    semester.FirstWeekProperty
            };

        foreach (var calendarWeek in
                 SemesterCalendar.GetWeeks(semester))
        {
            var projectionWeek =
                new BaseScheduleProjectionWeek
                {
                    WeekNumber =
                        calendarWeek.WeekNumber,
                    WeekStartDate =
                        calendarWeek.WeekStartDate,
                    WeekEndDate =
                        calendarWeek.WeekEndDate,
                    WeekProperty =
                        calendarWeek.WeekProperty
                };

            foreach (var baseLesson in baseLessons)
            {
                if (!SemesterCalendar.IsLessonIncluded(
                        baseLesson.WeekProperty,
                        calendarWeek.WeekProperty))
                {
                    continue;
                }

                DateTime lessonDate =
                    SemesterCalendar.GetLessonDate(
                        calendarWeek.WeekStartDate,
                        baseLesson.WeekDay);

                if (lessonDate <
                        semester.StartDate.Date ||
                    lessonDate >
                        semester.EndDate.Date)
                {
                    continue;
                }

                projectionWeek.Lessons.Add(
                    new BaseScheduleProjectionLesson
                    {
                        BaseLessonId = baseLesson.Id,
                        TeachingAssignmentId =
                            baseLesson
                                .TeachingAssignmentId,
                        LessonDate = lessonDate,
                        WeekDay = baseLesson.WeekDay,
                        LessonPosition =
                            baseLesson.LessonPosition,
                        SourceWeekProperty =
                            baseLesson.WeekProperty,
                        LessonType =
                            baseLesson.LessonType,
                        DisciplineId =
                            baseLesson.DisciplineId,
                        DisciplineName =
                            baseLesson.Discipline?.Name
                            ?? string.Empty,
                        TeacherId =
                            baseLesson.TeacherId,
                        TeacherName =
                            baseLesson.Teacher?.Name
                            ?? string.Empty,
                        ClassRoomId =
                            baseLesson.ClassRoomId,
                        ClassRoomName =
                            baseLesson.ClassRoom?.Name
                    });
            }

            projectionWeek.Lessons =
                projectionWeek.Lessons
                    .OrderBy(
                        lesson => lesson.LessonDate)
                    .ThenBy(
                        lesson =>
                            lesson.LessonPosition)
                    .ToList();

            response.Weeks.Add(projectionWeek);
        }

        return Ok(response);
    }

    private static int
        CalculateScheduledLessonCount(
            IEnumerable<BaseLesson> baseLessons,
            Semester semester)
    {
        int totalLessonCount = 0;
        var lessons = baseLessons.ToList();

        foreach (var calendarWeek in
                 SemesterCalendar.GetWeeks(semester))
        {
            foreach (var baseLesson in lessons)
            {
                if (!SemesterCalendar.IsLessonIncluded(
                        baseLesson.WeekProperty,
                        calendarWeek.WeekProperty))
                {
                    continue;
                }

                DateTime lessonDate =
                    SemesterCalendar.GetLessonDate(
                        calendarWeek.WeekStartDate,
                        baseLesson.WeekDay);

                if (lessonDate >=
                        semester.StartDate.Date &&
                    lessonDate <=
                        semester.EndDate.Date)
                {
                    totalLessonCount++;
                }
            }
        }

        return totalLessonCount;
    }
}
