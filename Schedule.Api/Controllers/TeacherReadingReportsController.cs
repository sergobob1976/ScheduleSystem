using Microsoft.AspNetCore.Mvc;
using Schedule.Core.Constants;
using Schedule.Core.DTOs;
using Schedule.Core.Enums;
using Schedule.Core.Interfaces;

namespace Schedule.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TeacherReadingReportsController : ControllerBase
{
    private readonly ISemesterRepository _semesterRepository;
    private readonly ITeacherRepository _teacherRepository;
    private readonly IRealLessonRepository _realLessonRepository;

    public TeacherReadingReportsController(
        ISemesterRepository semesterRepository,
        ITeacherRepository teacherRepository,
        IRealLessonRepository realLessonRepository)
    {
        _semesterRepository = semesterRepository;
        _teacherRepository = teacherRepository;
        _realLessonRepository = realLessonRepository;
    }

    [HttpGet("semester/{semesterId:int}/teacher/{teacherId:int}")]
    public async Task<ActionResult<TeacherReadingReportResponse>> GetReport(
        int semesterId,
        int teacherId,
        [FromQuery] DateTime periodStart,
        [FromQuery] DateTime periodEnd)
    {
        if (semesterId <= 0) return BadRequest(new { Message = "Потрібно обрати семестр." });
        if (teacherId <= 0) return BadRequest(new { Message = "Потрібно обрати викладача." });
        if (periodStart == default || periodEnd == default) return BadRequest(new { Message = "Потрібно вказати період звіту." });
        if (periodStart.Date > periodEnd.Date) return BadRequest(new { Message = "Початок періоду не може бути пізніше завершення." });

        var semester = await _semesterRepository.GetByIdAsync(semesterId);
        if (semester is null) return NotFound(new { Message = $"Семестр з ID {semesterId} не знайдено." });
        var teacher = await _teacherRepository.GetByIdAsync(teacherId);
        if (teacher is null) return NotFound(new { Message = $"Викладача з ID {teacherId} не знайдено." });
        if (periodStart.Date < semester.StartDate.Date || periodEnd.Date > semester.EndDate.Date)
            return BadRequest(new { Message = $"Період повинен бути в межах семестру: {semester.StartDate:dd.MM.yyyy}–{semester.EndDate:dd.MM.yyyy}." });

        var lessons = (await _realLessonRepository.GetByTeacherAndDateRangeAsync(
                teacherId, semesterId, periodStart.Date, periodEnd.Date))
            .Where(x => x.Status == RealLessonStatus.Completed)
            .ToList();

        var days = lessons
            .GroupBy(x => x.LessonDate.Date)
            .OrderBy(x => x.Key)
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

        return Ok(new TeacherReadingReportResponse
        {
            SemesterId = semester.Id,
            SemesterName = semester.Name,
            TeacherId = teacher.Id,
            TeacherName = teacher.Name,
            PeriodStart = periodStart.Date,
            PeriodEnd = periodEnd.Date,
            AcademicHoursPerLesson = ScheduleConstants.AcademicHoursPerLesson,
            TotalLessons = lessons.Count,
            TotalAcademicHours = lessons.Count * ScheduleConstants.AcademicHoursPerLesson,
            Days = days
        });
    }

    private static string GetDayName(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => "Понеділок", DayOfWeek.Tuesday => "Вівторок",
        DayOfWeek.Wednesday => "Середа", DayOfWeek.Thursday => "Четвер",
        DayOfWeek.Friday => "П’ятниця", DayOfWeek.Saturday => "Субота",
        DayOfWeek.Sunday => "Неділя", _ => string.Empty
    };
}
