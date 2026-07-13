using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schedule.Core.DTOs;
using Schedule.Core.Enums;
using Schedule.Core.Interfaces;
using Schedule.Core.Services;

namespace Schedule.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/student-schedule")]
public class StudentScheduleController : ControllerBase
{
    private readonly ISemesterRepository _semesters;
    private readonly IGroupRepository _groups;
    private readonly IGroupDisciplineRepository _groupDisciplines;
    private readonly IRealLessonRepository _lessons;

    public StudentScheduleController(
        ISemesterRepository semesters,
        IGroupRepository groups,
        IGroupDisciplineRepository groupDisciplines,
        IRealLessonRepository lessons)
    {
        _semesters = semesters;
        _groups = groups;
        _groupDisciplines = groupDisciplines;
        _lessons = lessons;
    }

    [HttpGet("options")]
    public async Task<ActionResult<StudentScheduleOptionsResponse>> GetOptions()
    {
        var semesters = (await _semesters.GetAllAsync()).OrderBy(item => item.StartDate).ToList();
        var groups = (await _groups.GetAllAsync()).ToDictionary(item => item.Id);
        var groupDisciplines = (await _groupDisciplines.GetAllAsync()).ToList();

        return Ok(new StudentScheduleOptionsResponse
        {
            Semesters = semesters.Select(semester => new StudentScheduleSemesterOption
            {
                Id = semester.Id,
                Name = semester.Name,
                StartDate = semester.StartDate,
                EndDate = semester.EndDate,
                Groups = groupDisciplines
                    .Where(item => item.SemesterId == semester.Id && groups.ContainsKey(item.GroupId))
                    .Select(item => groups[item.GroupId])
                    .DistinctBy(item => item.Id)
                    .OrderBy(item => item.Name)
                    .Select(item => new StudentScheduleGroupOption { Id = item.Id, Name = item.Name })
                    .ToList(),
                Weeks = SemesterCalendar.GetWeeks(semester)
                    .Select(week => new StudentScheduleWeekOption
                    {
                        WeekStartDate = week.WeekStartDate,
                        WeekEndDate = week.WeekEndDate,
                        ActiveStartDate = week.WeekStartDate < semester.StartDate.Date
                            ? semester.StartDate.Date
                            : week.WeekStartDate,
                        ActiveEndDate = week.WeekEndDate > semester.EndDate.Date
                            ? semester.EndDate.Date
                            : week.WeekEndDate
                    })
                    .ToList()
            }).ToList()
        });
    }

    [HttpGet("semester/{semesterId:int}/group/{groupId:int}/week/{weekStartDate:datetime}")]
    public async Task<ActionResult<IEnumerable<StudentScheduleLessonResponse>>> GetWeek(
        int semesterId,
        int groupId,
        DateTime weekStartDate)
    {
        var semester = await _semesters.GetByIdAsync(semesterId);
        if (semester is null)
            return NotFound(new { Message = "Семестр не знайдено." });
        if (weekStartDate.DayOfWeek != DayOfWeek.Monday)
            return BadRequest(new { Message = "Датою початку тижня має бути понеділок." });

        var groupIsAvailable = (await _groupDisciplines.GetBySemesterAndGroupAsync(semesterId, groupId)).Any();
        if (!groupIsAvailable)
            return NotFound(new { Message = "Групу в обраному семестрі не знайдено." });

        var endDate = weekStartDate.Date.AddDays(6);
        if (endDate < semester.StartDate.Date || weekStartDate.Date > semester.EndDate.Date)
            return BadRequest(new { Message = "Обраний тиждень не належить семестру." });

        var lessons = await _lessons.GetBySemesterAndDateRangeAsync(
            semesterId,
            weekStartDate.Date,
            endDate,
            groupId);

        return Ok(lessons
            .OrderBy(item => item.LessonDate)
            .ThenBy(item => item.LessonPosition)
            .Select(item => new StudentScheduleLessonResponse
            {
                LessonDate = item.LessonDate,
                LessonPosition = item.LessonPosition,
                DisciplineName = item.Discipline?.Name ?? "Дисципліну не вказано",
                TeacherName = TeacherNameFormatter.ToNameSurname(item.Teacher?.Name),
                LessonTypeName = GetLessonTypeName(item.LessonType),
                ClassRoomName = item.ClassRoom?.Name,
                Status = item.Status,
                ConferenceLink = NormalizePublicLink(item.ConferenceLink),
                ResourceLink = NormalizePublicLink(item.ResourceLink)
            }));
    }

    private static string GetLessonTypeName(LessonType value) => value switch
    {
        LessonType.Lecture => "Лекція",
        LessonType.Practical => "Практичне",
        LessonType.Laboratory => "Лабораторне",
        LessonType.Seminar => "Семінар",
        _ => "Інше"
    };

    private static string? NormalizePublicLink(string? value)
    {
        if (!Uri.TryCreate(value?.Trim(), UriKind.Absolute, out var uri)) return null;
        return uri.Scheme is "http" or "https" ? uri.AbsoluteUri : null;
    }
}
