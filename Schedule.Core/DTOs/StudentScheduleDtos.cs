using Schedule.Core.Enums;

namespace Schedule.Core.DTOs;

public class StudentScheduleOptionsResponse
{
    public List<StudentScheduleSemesterOption> Semesters { get; set; } = [];
}

public class StudentScheduleSemesterOption
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<StudentScheduleGroupOption> Groups { get; set; } = [];
    public List<StudentScheduleWeekOption> Weeks { get; set; } = [];
}

public class StudentScheduleGroupOption
{
    public int Id { get; set; }
    public required string Name { get; set; }
}

public class StudentScheduleWeekOption
{
    public DateTime WeekStartDate { get; set; }
    public DateTime WeekEndDate { get; set; }
    public DateTime ActiveStartDate { get; set; }
    public DateTime ActiveEndDate { get; set; }
}

public class StudentScheduleLessonResponse
{
    public DateTime LessonDate { get; set; }
    public int LessonPosition { get; set; }
    public required string DisciplineName { get; set; }
    public required string TeacherName { get; set; }
    public required string LessonTypeName { get; set; }
    public string? ClassRoomName { get; set; }
    public RealLessonStatus Status { get; set; }
    public string? ConferenceLink { get; set; }
    public string? ResourceLink { get; set; }
}
