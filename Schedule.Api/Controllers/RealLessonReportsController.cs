using Microsoft.AspNetCore.Mvc;
using Schedule.Core.DTOs;
using Schedule.Core.Interfaces;

namespace Schedule.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RealLessonReportsController
    : ControllerBase
{
    private const int AcademicHoursPerLesson = 2;

    private readonly IRealLessonReportRepository
        _reportRepository;

    private readonly ISemesterRepository
        _semesterRepository;

    private readonly IGroupRepository
        _groupRepository;

    public RealLessonReportsController(
        IRealLessonReportRepository reportRepository,
        ISemesterRepository semesterRepository,
        IGroupRepository groupRepository)
    {
        _reportRepository = reportRepository;
        _semesterRepository = semesterRepository;
        _groupRepository = groupRepository;
    }

    [HttpGet(
        "semester/{semesterId:int}/group/{groupId:int}/hours")]
    public async Task<
        ActionResult<RealLessonHoursReportResponse>>
        GetDisciplineHours(
            int semesterId,
            int groupId,
            [FromQuery] DateTime? reportDate = null)
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

        var group =
            await _groupRepository.GetByIdAsync(groupId);

        if (group == null)
        {
            return NotFound(new
            {
                Message =
                    $"Групу з ID {groupId} не знайдено."
            });
        }

        DateTime actualReportDate =
            (reportDate ?? DateTime.Today).Date;

        var disciplines =
            (
                await _reportRepository
                    .GetDisciplineHoursAsync(
                        semesterId,
                        groupId,
                        actualReportDate,
                        AcademicHoursPerLesson)
            ).ToList();

        return Ok(new RealLessonHoursReportResponse
        {
            SemesterId = semester.Id,
            SemesterName = semester.Name,
            GroupId = group.Id,
            GroupName = group.Name,
            ReportDate = actualReportDate,
            AcademicHoursPerLesson =
                AcademicHoursPerLesson,
            Disciplines = disciplines
        });
    }
}
