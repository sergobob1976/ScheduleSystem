using Microsoft.AspNetCore.Mvc;
using Schedule.Core.DTOs;
using Schedule.Core.Extensions;
using Schedule.Core.Interfaces;
using Schedule.Core.Services;

namespace Schedule.Api.Controllers;

[ApiController]
[Route("api/RealLessons")]
public class RealLessonWeekTransfersController
    : ControllerBase
{
    private readonly IRealLessonRepository
        _lessonRepository;

    private readonly ISemesterRepository
        _semesterRepository;

    public RealLessonWeekTransfersController(
        IRealLessonRepository lessonRepository,
        ISemesterRepository semesterRepository)
    {
        _lessonRepository = lessonRepository;
        _semesterRepository = semesterRepository;
    }

    [HttpGet(
        "transferred-weeks/semester/{semesterId:int}")]
    public async Task<
        ActionResult<IEnumerable<
            TransferredRealLessonWeekItem>>>
        GetTransferredWeeks(int semesterId)
    {
        if (semesterId <= 0)
        {
            return BadRequest(new
            {
                Message =
                    "Потрібно обрати семестр."
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

        var weeks =
            (
                await _lessonRepository
                    .GetTransferredWeeksAsync(semesterId)
            ).ToList();

        foreach (var week in weeks)
        {
            week.WeekPropertyName =
                week.WeekProperty
                    .ToUkranianString();
        }

        return Ok(weeks);
    }

    [HttpGet(
        "transfer-week-options/semester/{semesterId:int}")]
    public async Task<
        ActionResult<IEnumerable<
            RealLessonTransferWeekOption>>>
        GetTransferWeekOptions(int semesterId)
    {
        if (semesterId <= 0)
        {
            return BadRequest(new
            {
                Message =
                    "Потрібно обрати семестр."
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

        var transferredWeeks =
            (
                await _lessonRepository
                    .GetTransferredWeeksAsync(semesterId)
            )
            .ToDictionary(
                week => week.WeekStartDate.Date);

        var options =
            new List<RealLessonTransferWeekOption>();

        foreach (var calendarWeek in
                 SemesterCalendar.GetWeeks(semester))
        {
            transferredWeeks.TryGetValue(
                calendarWeek.WeekStartDate.Date,
                out var transferredWeek);

            options.Add(
                new RealLessonTransferWeekOption
                {
                    WeekNumber =
                        calendarWeek.WeekNumber,
                    WeekStartDate =
                        calendarWeek.WeekStartDate,
                    WeekEndDate =
                        calendarWeek.WeekEndDate,
                    ActiveStartDate =
                        calendarWeek.WeekStartDate <
                        semester.StartDate.Date
                            ? semester.StartDate.Date
                            : calendarWeek.WeekStartDate,
                    ActiveEndDate =
                        calendarWeek.WeekEndDate >
                        semester.EndDate.Date
                            ? semester.EndDate.Date
                            : calendarWeek.WeekEndDate,
                    RecommendedWeekProperty =
                        calendarWeek.WeekProperty,
                    RecommendedWeekPropertyName =
                        calendarWeek.WeekProperty
                            .ToUkranianString(),
                    IsTransferred =
                        transferredWeek != null,
                    TransferredWeekProperty =
                        transferredWeek?.WeekProperty,
                    TransferredWeekPropertyName =
                        transferredWeek?.WeekProperty
                            .ToUkranianString(),
                    TransferredLessonCount =
                        transferredWeek?.LessonCount ?? 0
                });
        }

        return Ok(options);
    }
}
