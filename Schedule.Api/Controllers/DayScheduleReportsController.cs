using Microsoft.AspNetCore.Mvc;
using Schedule.Api.Services;
using Schedule.Core.DTOs;
using Schedule.Core.Enums;
using Schedule.Core.Extensions;
using Schedule.Core.Interfaces;

namespace Schedule.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DayScheduleReportsController : ControllerBase
{
    private readonly ISemesterRepository _semesterRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IGroupDisciplineRepository _groupDisciplineRepository;
    private readonly IRealLessonRepository _realLessonRepository;
    private readonly DayScheduleDocxGenerator _docxGenerator;

    public DayScheduleReportsController(
        ISemesterRepository semesterRepository,
        IGroupRepository groupRepository,
        IGroupDisciplineRepository groupDisciplineRepository,
        IRealLessonRepository realLessonRepository,
        DayScheduleDocxGenerator docxGenerator)
    {
        _semesterRepository = semesterRepository;
        _groupRepository = groupRepository;
        _groupDisciplineRepository = groupDisciplineRepository;
        _realLessonRepository = realLessonRepository;
        _docxGenerator = docxGenerator;
    }

    [HttpGet("semester/{semesterId:int}")]
    public async Task<ActionResult<DayScheduleReportResponse>> GetReport(
        int semesterId,
        [FromQuery] DateTime scheduleDate)
    {
        var validation = await ValidateAsync(semesterId, scheduleDate);
        if (validation.Error is not null) return validation.Error;
        return Ok(await CreateReportAsync(validation.Semester!, scheduleDate.Date));
    }

    [HttpGet("semester/{semesterId:int}/docx")]
    public async Task<IActionResult> DownloadDocx(
        int semesterId,
        [FromQuery] DateTime scheduleDate)
    {
        var validation = await ValidateAsync(semesterId, scheduleDate);
        if (validation.Error is not null) return validation.Error;
        var report = await CreateReportAsync(validation.Semester!, scheduleDate.Date);
        var content = _docxGenerator.Generate(report);
        var fileName = $"Розклад-на-{scheduleDate:dd-MM-yyyy}.docx";
        return File(content, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", fileName);
    }

    private async Task<(Schedule.Core.Models.Semester? Semester, ActionResult? Error)> ValidateAsync(
        int semesterId,
        DateTime scheduleDate)
    {
        if (semesterId <= 0)
            return (null, BadRequest(new { Message = "Потрібно обрати семестр." }));
        if (scheduleDate == default)
            return (null, BadRequest(new { Message = "Потрібно вказати дату розкладу." }));

        var semester = await _semesterRepository.GetByIdAsync(semesterId);
        if (semester is null)
            return (null, NotFound(new { Message = $"Семестр з ID {semesterId} не знайдено." }));
        if (scheduleDate.Date < semester.StartDate.Date || scheduleDate.Date > semester.EndDate.Date)
            return (null, BadRequest(new { Message = $"Дата повинна бути в межах семестру: {semester.StartDate:dd.MM.yyyy}–{semester.EndDate:dd.MM.yyyy}." }));

        return (semester, null);
    }

    private async Task<DayScheduleReportResponse> CreateReportAsync(
        Schedule.Core.Models.Semester semester,
        DateTime scheduleDate)
    {
        var groupDisciplines = (await _groupDisciplineRepository.GetBySemesterIdAsync(semester.Id)).ToList();
        var groupIds = groupDisciplines.Select(x => x.GroupId).ToHashSet();
        var lessons = (await _realLessonRepository.GetBySemesterAndDateRangeAsync(
            semester.Id, scheduleDate, scheduleDate)).ToList();
        groupIds.UnionWith(lessons.Select(x => x.GroupId));

        var groups = (await _groupRepository.GetAllAsync())
            .Where(x => groupIds.Contains(x.Id))
            .OrderBy(x => x.Name)
            .ToList();

        return new DayScheduleReportResponse
        {
            SemesterId = semester.Id,
            SemesterName = semester.Name,
            ScheduleDate = scheduleDate,
            DayName = GetDayName(scheduleDate.DayOfWeek),
            Groups = groups.Select(group => new DayScheduleGroupItem
            {
                GroupId = group.Id,
                GroupName = group.Name,
                Lessons = lessons
                    .Where(x => x.GroupId == group.Id)
                    .OrderBy(x => x.LessonPosition)
                    .Select(x => new DayScheduleLessonItem
                    {
                        LessonPosition = x.LessonPosition,
                        DisciplineName = x.Discipline?.Name ?? "Дисципліну не вказано",
                        TeacherName = x.Teacher?.Name ?? "Викладача не вказано",
                        ClassRoomName = x.ClassRoom?.Name,
                        ConferenceLink = x.ConferenceLink,
                        ResourceLink = x.ResourceLink,
                        LessonType = x.LessonType,
                        Status = x.Status
                    })
                    .ToList()
            }).ToList()
        };
    }

    private static string GetDayName(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => "Понеділок",
        DayOfWeek.Tuesday => "Вівторок",
        DayOfWeek.Wednesday => "Середа",
        DayOfWeek.Thursday => "Четвер",
        DayOfWeek.Friday => "П’ятниця",
        DayOfWeek.Saturday => "Субота",
        DayOfWeek.Sunday => "Неділя",
        _ => string.Empty
    };
}
