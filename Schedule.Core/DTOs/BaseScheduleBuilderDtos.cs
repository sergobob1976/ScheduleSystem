using Schedule.Core.Enums;

namespace Schedule.Core.DTOs;

public class BaseScheduleBuilderResponse
{
    public int SemesterId { get; set; }

    public string SemesterName { get; set; } =
        string.Empty;

    public DateTime SemesterStartDate { get; set; }

    public DateTime SemesterEndDate { get; set; }

    public WeekProperty FirstWeekProperty { get; set; }

    public int GroupId { get; set; }

    public string GroupName { get; set; } =
        string.Empty;

    public int AcademicHoursPerLesson { get; set; }

    public List<BaseScheduleDisciplineItem>
        Disciplines
    { get; set; } = [];
}

public class BaseScheduleDisciplineItem
{
    public int GroupDisciplineId { get; set; }

    public int DisciplineId { get; set; }

    public string DisciplineName { get; set; } =
        string.Empty;

    public int TotalPlannedHours { get; set; }

    public int LectureHours { get; set; }

    public int PracticalHours { get; set; }

    public int LaboratoryHours { get; set; }

    public int SeminarHours { get; set; }

    public int OtherHours { get; set; }

    public int TotalAssignedHours { get; set; }

    public int UnassignedHours { get; set; }

    public int OverAssignedHours { get; set; }

    public int ScheduledLessonCount { get; set; }

    public int ScheduledHours { get; set; }

    public int RemainingScheduledHours { get; set; }

    public int ExceededScheduledHours { get; set; }

    public bool IsScheduleExceeded { get; set; }

    public List<BaseScheduleAssignmentItem>
        TeachingAssignments
    { get; set; } = [];
}

public class BaseScheduleAssignmentItem
{
    public int TeachingAssignmentId { get; set; }

    public int TeacherId { get; set; }

    public string TeacherName { get; set; } =
        string.Empty;

    public LessonType LessonType { get; set; }

    public int AssignedHours { get; set; }

    public int ScheduledLessonCount { get; set; }

    public int ScheduledHours { get; set; }

    public int RemainingHours { get; set; }

    public int ExceededHours { get; set; }

    public bool IsExceeded { get; set; }
}

public class BaseScheduleProjectionResponse
{
    public int SemesterId { get; set; }

    public string SemesterName { get; set; } =
        string.Empty;

    public int GroupId { get; set; }

    public string GroupName { get; set; } =
        string.Empty;

    public WeekProperty FirstWeekProperty { get; set; }

    public List<BaseScheduleProjectionWeek> Weeks { get; set; } =
        [];
}

public class BaseScheduleProjectionWeek
{
    public int WeekNumber { get; set; }

    public DateTime WeekStartDate { get; set; }

    public DateTime WeekEndDate { get; set; }

    public WeekProperty WeekProperty { get; set; }

    public List<BaseScheduleProjectionLesson> Lessons { get; set; } =
        [];
}

public class BaseScheduleProjectionLesson
{
    public int BaseLessonId { get; set; }

    public int? TeachingAssignmentId { get; set; }

    public DateTime LessonDate { get; set; }

    public WeekDay WeekDay { get; set; }

    public int LessonPosition { get; set; }

    public WeekProperty SourceWeekProperty { get; set; }

    public LessonType LessonType { get; set; }

    public int DisciplineId { get; set; }

    public string DisciplineName { get; set; } =
        string.Empty;

    public int TeacherId { get; set; }

    public string TeacherName { get; set; } =
        string.Empty;

    public int? ClassRoomId { get; set; }

    public string? ClassRoomName { get; set; }
}
