using Schedule.Core.Enums;

namespace Schedule.Core.DTOs;

public class MobileScheduleOptionsResponse
{
    public List<MobileScheduleFilterOption> Groups { get; set; } = [];
    public List<MobileScheduleFilterOption> Teachers { get; set; } = [];
}

public class MobileScheduleFilterOption
{
    public int Id { get; set; }
    public required string Name { get; set; }
}

public class MobileScheduleLessonResponse
{
    public DateTime LessonDate { get; set; }
    public int LessonPosition { get; set; }
    public required string DisciplineName { get; set; }
    public required string LessonTypeName { get; set; }
    public required string GroupName { get; set; }
    public required string TeacherName { get; set; }
    public string? ClassRoomName { get; set; }
    public RealLessonStatus Status { get; set; }
    public string? ConferenceLink { get; set; }
    public string? ResourceLink { get; set; }
}
