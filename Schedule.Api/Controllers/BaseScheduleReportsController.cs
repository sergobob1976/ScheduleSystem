using Microsoft.AspNetCore.Mvc;
using Schedule.Core.DTOs;
using Schedule.Core.Interfaces;
using Schedule.Core.Constants;

namespace Schedule.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BaseScheduleReportsController
    : ControllerBase
{
    private readonly IBaseScheduleReportRepository
        _reportRepository;

    public BaseScheduleReportsController(
        IBaseScheduleReportRepository reportRepository)
    {
        _reportRepository = reportRepository;
    }

    [HttpGet(
        "semester/{semesterId:int}/teacher/{teacherId:int}/hours")]
    public async Task<
        ActionResult<BaseTeacherHoursReportResponse>>
        GetTeacherHours(
            int semesterId,
            int teacherId)
    {
        if (semesterId <= 0)
        {
            return BadRequest(new
            {
                Message =
                    "Потрібно обрати семестр."
            });
        }

        if (teacherId <= 0)
        {
            return BadRequest(new
            {
                Message =
                    "Потрібно обрати викладача."
            });
        }

        var report =
            await _reportRepository.GetTeacherHoursAsync(
                semesterId,
                teacherId,
                ScheduleConstants
                    .AcademicHoursPerLesson);

        if (report == null)
        {
            return NotFound(new
            {
                Message =
                    "Планове навантаження викладача " +
                    "на цей семестр не знайдено."
            });
        }

        return Ok(report);
    }
}
