using Schedule.Core.DTOs;

namespace Schedule.Core.Interfaces;

public interface IRealLessonReportRepository
{
    Task<IEnumerable<RealLessonDisciplineHoursItem>>
        GetDisciplineHoursAsync(
            int semesterId,
            int groupId,
            DateTime reportDate,
            int academicHoursPerLesson);
}
