using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schedule.Core.DTOs;
using Schedule.Core.Enums;
using Schedule.Core.Interfaces;
using Schedule.Core.Services;

namespace Schedule.Api.Controllers;

[ApiController]
[Authorize(Roles = "Teacher")]
[Route("api/teacher-schedule")]
public class TeacherScheduleController : ControllerBase
{
    private readonly ISemesterRepository _semesters;
    private readonly IRealLessonRepository _lessons;

    public TeacherScheduleController(
        ISemesterRepository semesters,
        IRealLessonRepository lessons)
    {
        _semesters = semesters;
        _lessons = lessons;
    }

    [HttpGet("options")]
    public async Task<ActionResult<TeacherScheduleOptionsResponse>> GetOptions()
    {
        var semesters = (await _semesters.GetAllAsync()).OrderBy(item => item.StartDate);
        return Ok(new TeacherScheduleOptionsResponse
        {
            TeacherName = User.FindFirstValue(ClaimTypes.GivenName) ?? "Викладач",
            Semesters = semesters.Select(semester => new TeacherScheduleSemesterOption
            {
                Id = semester.Id,
                Name = semester.Name,
                StartDate = semester.StartDate,
                EndDate = semester.EndDate,
                Weeks = SemesterCalendar.GetWeeks(semester).Select(week => new StudentScheduleWeekOption
                {
                    WeekStartDate = week.WeekStartDate,
                    WeekEndDate = week.WeekEndDate,
                    ActiveStartDate = week.WeekStartDate < semester.StartDate.Date
                        ? semester.StartDate.Date
                        : week.WeekStartDate,
                    ActiveEndDate = week.WeekEndDate > semester.EndDate.Date
                        ? semester.EndDate.Date
                        : week.WeekEndDate
                }).ToList()
            }).ToList()
        });
    }

    [HttpGet("semester/{semesterId:int}/week/{weekStartDate:datetime}")]
    public async Task<ActionResult<IEnumerable<TeacherScheduleLessonResponse>>> GetWeek(
        int semesterId,
        DateTime weekStartDate)
    {
        var teacherId = GetTeacherId();
        if (teacherId is null) return Forbid();

        var semester = await _semesters.GetByIdAsync(semesterId);
        if (semester is null)
            return NotFound(new { Message = "Семестр не знайдено." });
        if (weekStartDate.DayOfWeek != DayOfWeek.Monday)
            return BadRequest(new { Message = "Датою початку тижня має бути понеділок." });

        var endDate = weekStartDate.Date.AddDays(6);
        if (endDate < semester.StartDate.Date || weekStartDate.Date > semester.EndDate.Date)
            return BadRequest(new { Message = "Обраний тиждень не належить семестру." });

        var lessons = await _lessons.GetByTeacherAndDateRangeAsync(
            teacherId.Value,
            semesterId,
            weekStartDate.Date,
            endDate);

        return Ok(lessons
            .OrderBy(item => item.LessonDate)
            .ThenBy(item => item.LessonPosition)
            .Select(ToResponse));
    }

    [HttpPatch("lessons/{lessonId:int}/links")]
    public async Task<IActionResult> UpdateLinks(
        int lessonId,
        UpdateTeacherLessonLinksRequest request)
    {
        var teacherId = GetTeacherId();
        if (teacherId is null) return Forbid();

        var lesson = await _lessons.GetByIdAsync(lessonId);
        if (lesson is null || lesson.TeacherId != teacherId.Value)
            return NotFound(new { Message = "Заняття не знайдено у вашому розкладі." });

        if (!IsValidLink(request.ConferenceLink) || !IsValidLink(request.ResourceLink))
            return BadRequest(new { Message = "Посилання повинно починатися з http:// або https://." });
        if (request.ConferenceLink?.Trim().Length > 500 || request.ResourceLink?.Trim().Length > 500)
            return BadRequest(new { Message = "Посилання не може містити більше 500 символів." });

        var updated = await _lessons.UpdateLinksAsync(
            lessonId,
            Normalize(request.ConferenceLink),
            Normalize(request.ResourceLink));

        return updated
            ? NoContent()
            : NotFound(new { Message = "Заняття не знайдено." });
    }

    private int? GetTeacherId() =>
        int.TryParse(User.FindFirstValue("teacher_id"), out var teacherId)
            ? teacherId
            : null;

    private static TeacherScheduleLessonResponse ToResponse(Schedule.Core.Models.RealLesson lesson) => new()
    {
        Id = lesson.Id,
        LessonDate = lesson.LessonDate,
        LessonPosition = lesson.LessonPosition,
        GroupName = lesson.Group?.Name ?? "Групу не вказано",
        DisciplineName = lesson.Discipline?.Name ?? "Дисципліну не вказано",
        LessonTypeName = lesson.LessonType switch
        {
            LessonType.Lecture => "Лекція",
            LessonType.Practical => "Практичне",
            LessonType.Laboratory => "Лабораторне",
            LessonType.Seminar => "Семінар",
            _ => "Інше"
        },
        ClassRoomName = lesson.ClassRoom?.Name,
        Status = lesson.Status,
        ConferenceLink = Normalize(lesson.ConferenceLink),
        ResourceLink = Normalize(lesson.ResourceLink)
    };

    private static bool IsValidLink(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return true;
        return Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) &&
               uri.Scheme is "http" or "https";
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
