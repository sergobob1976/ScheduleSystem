using Schedule.Core.Enums;

namespace Schedule.Core.DTOs;

public class DayScheduleReportResponse
{
    public int SemesterId { get; set; }
    public string SemesterName { get; set; } = string.Empty;
    public DateTime ScheduleDate { get; set; }
    public string DayName { get; set; } = string.Empty;
    public List<DayScheduleGroupItem> Groups { get; set; } = [];
}

public class DayScheduleGroupItem
{
    public int GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public List<DayScheduleLessonItem> Lessons { get; set; } = [];
}

public class DayScheduleLessonItem
{
    public int LessonPosition { get; set; }
    public string DisciplineName { get; set; } = string.Empty;
    public string TeacherName { get; set; } = string.Empty;
    public string? TeacherEmail { get; set; }
    public string? ClassRoomName { get; set; }
    public string? ConferenceLink { get; set; }
    public string? ResourceLink { get; set; }
    public LessonType LessonType { get; set; }
    public RealLessonStatus Status { get; set; }
}
