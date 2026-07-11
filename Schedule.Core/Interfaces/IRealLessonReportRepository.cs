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

    Task<IEnumerable<TeacherDisciplineHoursItem>>
        GetTeacherDisciplineHoursAsync(
            int semesterId,
            int teacherId,
            DateTime reportDate,
            int academicHoursPerLesson);
}
