using Microsoft.AspNetCore.Mvc;
using Schedule.Api.Services;
using Schedule.Core.Constants;
using Schedule.Core.DTOs;
using Schedule.Core.Enums;
using Schedule.Core.Interfaces;
using Schedule.Core.Models;
using Schedule.Core.Services;

namespace Schedule.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TeacherReadingReportsController : ControllerBase
{
    private readonly ISemesterRepository _semesterRepository;
    private readonly ITeacherRepository _teacherRepository;
    private readonly IRealLessonRepository _realLessonRepository;
    private readonly TeacherReadingDocxGenerator _docxGenerator;

    public TeacherReadingReportsController(
        ISemesterRepository semesterRepository,
        ITeacherRepository teacherRepository,
        IRealLessonRepository realLessonRepository,
        TeacherReadingDocxGenerator docxGenerator)
    {
        _semesterRepository = semesterRepository;
        _teacherRepository = teacherRepository;
        _realLessonRepository = realLessonRepository;
        _docxGenerator = docxGenerator;
    }

    [HttpGet("semester/{semesterId:int}/teacher/{teacherId:int}")]
    public async Task<ActionResult<TeacherReadingReportResponse>> GetReport(
        int semesterId, int teacherId,
        [FromQuery] DateTime periodStart,
        [FromQuery] DateTime periodEnd)
    {
        var validation = await ValidateAsync(semesterId, teacherId, periodStart, periodEnd);
        if (validation.Error is not null) return validation.Error;
        return Ok(await CreateReportAsync(validation.Semester!, validation.Teacher!, periodStart.Date, periodEnd.Date));
    }

    [HttpGet("semester/{semesterId:int}/teacher/{teacherId:int}/docx")]
    public async Task<IActionResult> DownloadDocx(
        int semesterId, int teacherId,
        [FromQuery] DateTime periodStart,
        [FromQuery] DateTime periodEnd)
    {
        var validation = await ValidateAsync(semesterId, teacherId, periodStart, periodEnd);
        if (validation.Error is not null) return validation.Error;
        var report = await CreateReportAsync(validation.Semester!, validation.Teacher!, periodStart.Date, periodEnd.Date);
        var content = _docxGenerator.Generate(report);
        return File(content, "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            $"Вичитка-{MakeSafeFileName(report.TeacherName)}-{periodStart:dd-MM-yyyy}-{periodEnd:dd-MM-yyyy}.docx");
    }

    private async Task<(Semester? Semester, Teacher? Teacher, ActionResult? Error)> ValidateAsync(
        int semesterId, int teacherId, DateTime periodStart, DateTime periodEnd)
    {
        if (semesterId <= 0) return (null, null, BadRequest(new { Message = "Потрібно обрати семестр." }));
        if (teacherId <= 0) return (null, null, BadRequest(new { Message = "Потрібно обрати викладача." }));
        if (periodStart == default || periodEnd == default) return (null, null, BadRequest(new { Message = "Потрібно вказати період звіту." }));
        if (periodStart.Date > periodEnd.Date) return (null, null, BadRequest(new { Message = "Початок періоду не може бути пізніше завершення." }));

        var semester = await _semesterRepository.GetByIdAsync(semesterId);
        if (semester is null) return (null, null, NotFound(new { Message = $"Семестр з ID {semesterId} не знайдено." }));
        var teacher = await _teacherRepository.GetByIdAsync(teacherId);
        if (teacher is null) return (null, null, NotFound(new { Message = $"Викладача з ID {teacherId} не знайдено." }));
        if (periodStart.Date < semester.StartDate.Date || periodEnd.Date > semester.EndDate.Date)
            return (null, null, BadRequest(new { Message = $"Період повинен бути в межах семестру: {semester.StartDate:dd.MM.yyyy}–{semester.EndDate:dd.MM.yyyy}." }));
        return (semester, teacher, null);
    }

    private async Task<TeacherReadingReportResponse> CreateReportAsync(
        Semester semester, Teacher teacher, DateTime periodStart, DateTime periodEnd)
    {
        var lessons = (await _realLessonRepository.GetByTeacherAndDateRangeAsync(
                teacher.Id, semester.Id, periodStart, periodEnd))
            .Where(x => x.Status == RealLessonStatus.Completed).ToList();
        var days = lessons.GroupBy(x => x.LessonDate.Date).OrderBy(x => x.Key)
            .Select(day => new TeacherReadingDayItem
            {
                LessonDate = day.Key,
                DayName = GetDayName(day.Key.DayOfWeek),
                Lessons = day.OrderBy(x => x.LessonPosition).ThenBy(x => x.Group?.Name)
                    .Select(x => new TeacherReadingLessonItem
                    {
                        LessonPosition = x.LessonPosition,
                        GroupId = x.GroupId,
                        GroupName = x.Group?.Name ?? "Групу не вказано",
                        DisciplineId = x.DisciplineId,
                        DisciplineName = x.Discipline?.Name ?? "Дисципліну не вказано",
                        LessonType = x.LessonType,
                        ClassRoomName = x.ClassRoom?.Name
                    }).ToList()
            }).ToList();

        return new TeacherReadingReportResponse
        {
            SemesterId = semester.Id, SemesterName = semester.Name,
            TeacherId = teacher.Id, TeacherName = TeacherNameFormatter.ToNameSurname(teacher.Name),
            PeriodStart = periodStart, PeriodEnd = periodEnd,
            AcademicHoursPerLesson = ScheduleConstants.AcademicHoursPerLesson,
            TotalLessons = lessons.Count,
            TotalAcademicHours = lessons.Count * ScheduleConstants.AcademicHoursPerLesson,
            Days = days
        };
    }

    private static string MakeSafeFileName(string value) =>
        string.Concat(value.Select(character => Path.GetInvalidFileNameChars().Contains(character) ? '-' : character));

    private static string GetDayName(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => "Понеділок", DayOfWeek.Tuesday => "Вівторок",
        DayOfWeek.Wednesday => "Середа", DayOfWeek.Thursday => "Четвер",
        DayOfWeek.Friday => "П’ятниця", DayOfWeek.Saturday => "Субота",
        DayOfWeek.Sunday => "Неділя", _ => string.Empty
    };
}
