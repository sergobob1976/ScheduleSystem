using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schedule.Core.DTOs;
using Schedule.Core.Enums;
using Schedule.Core.Interfaces;
using Schedule.Core.Models;
using Schedule.Core.Services;

namespace Schedule.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/mobile-schedule")]
public class MobileScheduleController : ControllerBase
{
    private readonly IGroupRepository _groups;
    private readonly ITeacherRepository _teachers;
    private readonly IRealLessonRepository _lessons;

    public MobileScheduleController(
        IGroupRepository groups,
        ITeacherRepository teachers,
        IRealLessonRepository lessons)
    {
        _groups = groups;
        _teachers = teachers;
        _lessons = lessons;
    }

    [HttpGet("options")]
    public async Task<ActionResult<MobileScheduleOptionsResponse>> GetOptions()
    {
        var groups = await _groups.GetAllAsync();
        var teachers = await _teachers.GetAllAsync();

        return Ok(new MobileScheduleOptionsResponse
        {
            Groups = groups
                .OrderBy(item => item.Name)
                .Select(item => new MobileScheduleFilterOption
                {
                    Id = item.Id,
                    Name = item.Name
                })
                .ToList(),
            Teachers = teachers
                .OrderBy(item => item.Name)
                .Select(item => new MobileScheduleFilterOption
                {
                    Id = item.Id,
                    Name = TeacherNameFormatter.ToNameSurname(item.Name)
                })
                .ToList()
        });
    }

    [HttpGet("group/{groupId:int}/date/{date:datetime}")]
    public async Task<ActionResult<IEnumerable<MobileScheduleLessonResponse>>> GetForGroup(
        int groupId,
        DateTime date)
    {
        if (await _groups.GetByIdAsync(groupId) is null)
        {
            return NotFound(new { Message = "Групу не знайдено." });
        }

        return Ok((await _lessons.GetByGroupAndDateAsync(groupId, date.Date))
            .OrderBy(item => item.LessonPosition)
            .Select(ToResponse));
    }

    [HttpGet("teacher/{teacherId:int}/date/{date:datetime}")]
    public async Task<ActionResult<IEnumerable<MobileScheduleLessonResponse>>> GetForTeacher(
        int teacherId,
        DateTime date)
    {
        if (await _teachers.GetByIdAsync(teacherId) is null)
        {
            return NotFound(new { Message = "Викладача не знайдено." });
        }

        return Ok((await _lessons.GetByTeacherAndDateAsync(teacherId, date.Date))
            .OrderBy(item => item.LessonPosition)
            .ThenBy(item => item.Group?.Name)
            .Select(ToResponse));
    }

    private static MobileScheduleLessonResponse ToResponse(RealLesson lesson) => new()
    {
        LessonDate = lesson.LessonDate,
        LessonPosition = lesson.LessonPosition,
        DisciplineName = lesson.Discipline?.Name ?? "Дисципліну не вказано",
        LessonTypeName = GetLessonTypeName(lesson.LessonType),
        GroupName = lesson.Group?.Name ?? "Групу не вказано",
        TeacherName = TeacherNameFormatter.ToNameSurname(lesson.Teacher?.Name),
        ClassRoomName = lesson.ClassRoom?.Name,
        Status = lesson.Status,
        ConferenceLink = NormalizePublicLink(lesson.ConferenceLink),
        ResourceLink = NormalizePublicLink(lesson.ResourceLink)
    };

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
        if (!Uri.TryCreate(value?.Trim(), UriKind.Absolute, out var uri))
        {
            return null;
        }

        return uri.Scheme is "http" or "https" ? uri.AbsoluteUri : null;
    }
}
