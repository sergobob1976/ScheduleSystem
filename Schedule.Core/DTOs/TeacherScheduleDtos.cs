using Schedule.Core.Enums;

namespace Schedule.Core.DTOs;

public class TeacherScheduleOptionsResponse
{
    public required string TeacherName { get; set; }
    public List<TeacherScheduleSemesterOption> Semesters { get; set; } = [];
}

public class TeacherScheduleSemesterOption
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<StudentScheduleWeekOption> Weeks { get; set; } = [];
}

public class TeacherScheduleLessonResponse
{
    public int Id { get; set; }
    public DateTime LessonDate { get; set; }
    public int LessonPosition { get; set; }
    public required string GroupName { get; set; }
    public required string DisciplineName { get; set; }
    public required string LessonTypeName { get; set; }
    public string? ClassRoomName { get; set; }
    public RealLessonStatus Status { get; set; }
    public string? ConferenceLink { get; set; }
    public string? ResourceLink { get; set; }
}

public class UpdateTeacherLessonLinksRequest
{
    public string? ConferenceLink { get; set; }
    public string? ResourceLink { get; set; }
}
