using Schedule.Core.Enums;

namespace Schedule.Core.DTOs;

public class TeacherReadingReportResponse
{
    public int SemesterId { get; set; }
    public string SemesterName { get; set; } = string.Empty;
    public int TeacherId { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public int AcademicHoursPerLesson { get; set; }
    public int TotalLessons { get; set; }
    public int TotalAcademicHours { get; set; }
    public List<TeacherReadingDayItem> Days { get; set; } = [];
}

public class TeacherReadingDayItem
{
    public DateTime LessonDate { get; set; }
    public string DayName { get; set; } = string.Empty;
    public List<TeacherReadingLessonItem> Lessons { get; set; } = [];
}

public class TeacherReadingLessonItem
{
    public int LessonPosition { get; set; }
    public int GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public int DisciplineId { get; set; }
    public string DisciplineName { get; set; } = string.Empty;
    public LessonType LessonType { get; set; }
    public string? ClassRoomName { get; set; }
}
