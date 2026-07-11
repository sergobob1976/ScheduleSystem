namespace Schedule.Core.DTOs;

public class RealLessonHoursReportResponse
{
    public int SemesterId { get; set; }

    public string SemesterName { get; set; } =
        string.Empty;

    public int GroupId { get; set; }

    public string GroupName { get; set; } =
        string.Empty;

    public DateTime ReportDate { get; set; }

    public int AcademicHoursPerLesson { get; set; }

    public List<RealLessonDisciplineHoursItem>
        Disciplines
    { get; set; } = [];
}

public class RealLessonDisciplineHoursItem
{
    public int GroupDisciplineId { get; set; }

    public int DisciplineId { get; set; }

    public string DisciplineName { get; set; } =
        string.Empty;

    public int PlannedHours { get; set; }

    public int CompletedLessonCount { get; set; }

    public int CompletedHours { get; set; }

    public int PlannedFutureLessonCount { get; set; }

    public int PlannedFutureHours { get; set; }

    public int UnconfirmedPastLessonCount { get; set; }

    public int UnconfirmedPastHours { get; set; }

    public int CancelledLessonCount { get; set; }

    public int CancelledHours { get; set; }

    public int RemainingHours { get; set; }

    public int ExceededHours { get; set; }

    public bool IsExceeded { get; set; }
}

public class TeacherLessonHoursReportResponse
{
    public int SemesterId { get; set; }

    public string SemesterName { get; set; } =
        string.Empty;

    public int TeacherId { get; set; }

    public string TeacherName { get; set; } =
        string.Empty;

    public DateTime ReportDate { get; set; }

    public int AcademicHoursPerLesson { get; set; }

    public List<TeacherDisciplineHoursItem>
        Disciplines
    { get; set; } = [];
}

public class TeacherDisciplineHoursItem
{
    public int TeacherDisciplineLoadId { get; set; }

    public int DisciplineId { get; set; }

    public string DisciplineName { get; set; } =
        string.Empty;

    public int PlannedHours { get; set; }

    public int CompletedLessonCount { get; set; }

    public int CompletedHours { get; set; }

    public int PlannedFutureLessonCount { get; set; }

    public int PlannedFutureHours { get; set; }

    public int UnconfirmedPastLessonCount { get; set; }

    public int UnconfirmedPastHours { get; set; }

    public int CancelledLessonCount { get; set; }

    public int CancelledHours { get; set; }

    public int RemainingHours { get; set; }

    public int ExceededHours { get; set; }

    public bool IsExceeded { get; set; }
}
