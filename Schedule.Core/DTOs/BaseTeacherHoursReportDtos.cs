namespace Schedule.Core.DTOs;

public class BaseTeacherHoursReportResponse
{
    public int SemesterId { get; set; }

    public string SemesterName { get; set; } =
        string.Empty;

    public int TeacherId { get; set; }

    public string TeacherName { get; set; } =
        string.Empty;

    public int AcademicHoursPerLesson { get; set; }

    public int PlannedHours { get; set; }

    public int AssignedHours { get; set; }

    public int UnassignedHours { get; set; }

    public int OverAssignedHours { get; set; }

    public int ScheduledHours { get; set; }

    public int RemainingScheduledHours { get; set; }

    public int ExceededScheduledHours { get; set; }

    public bool IsScheduleExceeded { get; set; }

    public List<BaseTeacherDisciplineHoursItem>
        Disciplines
    { get; set; } = [];
}

public class BaseTeacherDisciplineHoursItem
{
    public int TeacherDisciplineLoadId { get; set; }

    public int DisciplineId { get; set; }

    public string DisciplineName { get; set; } =
        string.Empty;

    public int PlannedHours { get; set; }

    public int AssignedHours { get; set; }

    public int UnassignedHours { get; set; }

    public int OverAssignedHours { get; set; }

    public int ScheduledLessonCount { get; set; }

    public int ScheduledHours { get; set; }

    public int RemainingScheduledHours { get; set; }

    public int ExceededScheduledHours { get; set; }

    public bool IsScheduleExceeded { get; set; }
}
