using Microsoft.AspNetCore.Mvc;
using Schedule.Core.DTOs;
using Schedule.Core.Enums;
using Schedule.Core.Interfaces;
using Schedule.Core.Models;

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

            response.Disciplines.Add(
                disciplineItem);
        }

        return Ok(response);
    }

    private static int
        CalculateScheduledLessonCount(
            IEnumerable<BaseLesson> baseLessons,
            Semester semester)
    {
        int totalLessonCount = 0;

        foreach (var baseLesson in baseLessons)
        {
            totalLessonCount +=
                CountOccurrencesInSemester(
                    baseLesson,
                    semester);
        }

        return totalLessonCount;
    }

    private static int CountOccurrencesInSemester(
        BaseLesson baseLesson,
        Semester semester)
    {
        DateTime semesterStart =
            semester.StartDate.Date;

        DateTime semesterEnd =
            semester.EndDate.Date;

        int occurrences = 0;

        for (
            DateTime currentDate = semesterStart;
            currentDate <= semesterEnd;
            currentDate = currentDate.AddDays(1))
        {
            if (!IsMatchingWeekDay(
                    currentDate,
                    baseLesson.WeekDay))
            {
                continue;
            }

            int weekIndex =
                (currentDate - semesterStart).Days / 7;

            bool isNumeratorWeek =
                weekIndex % 2 == 0;

            bool shouldInclude =
                baseLesson.WeekProperty switch
                {
                    WeekProperty.EveryWeek =>
                        true,

                    WeekProperty.Numerator =>
                        isNumeratorWeek,

                    WeekProperty.Denominator =>
                        !isNumeratorWeek,

                    _ =>
                        false
                };

            if (shouldInclude)
            {
                occurrences++;
            }
        }

        return occurrences;
    }

    private static bool IsMatchingWeekDay(
        DateTime date,
        WeekDay weekDay)
    {
        DayOfWeek expectedDay =
            weekDay switch
            {
                WeekDay.Monday =>
                    DayOfWeek.Monday,

                WeekDay.Tuesday =>
                    DayOfWeek.Tuesday,

                WeekDay.Wednesday =>
                    DayOfWeek.Wednesday,

                WeekDay.Thursday =>
                    DayOfWeek.Thursday,

                WeekDay.Friday =>
                    DayOfWeek.Friday,

                WeekDay.Saturday =>
                    DayOfWeek.Saturday,

                WeekDay.Sunday =>
                    DayOfWeek.Sunday,

                _ =>
                    throw new ArgumentOutOfRangeException(
                        nameof(weekDay),
                        weekDay,
                        "Невідомий день тижня.")
            };

        return date.DayOfWeek == expectedDay;
    }
}