using Schedule.Core.DTOs;

namespace Schedule.Core.Interfaces;

public interface IBaseScheduleReportRepository
{
    Task<BaseTeacherHoursReportResponse?>
        GetTeacherHoursAsync(
            int semesterId,
            int teacherId,
            int academicHoursPerLesson);
}
