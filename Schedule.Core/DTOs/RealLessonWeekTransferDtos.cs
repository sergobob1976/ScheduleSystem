using Schedule.Core.Enums;

namespace Schedule.Core.DTOs;

public class TransferRealLessonWeekRequest
{
    public int SemesterId { get; set; }

    public DateTime WeekStartDate { get; set; }

    public WeekProperty WeekProperty { get; set; }
}

public class TransferRealLessonWeekResponse
{
    public int SemesterId { get; set; }

    public DateTime WeekStartDate { get; set; }

    public DateTime WeekEndDate { get; set; }

    public WeekProperty WeekProperty { get; set; }

    public int CreatedLessonCount { get; set; }

    public string Message { get; set; } =
        string.Empty;
}

public enum TransferRealLessonWeekResult
{
    Created = 1,
    AlreadyTransferred = 2
}

public class TransferredRealLessonWeekItem
{
    public int Id { get; set; }

    public int SemesterId { get; set; }

    public DateTime WeekStartDate { get; set; }

    public DateTime WeekEndDate { get; set; }

    public WeekProperty WeekProperty { get; set; }

    public string WeekPropertyName { get; set; } =
        string.Empty;

    public int LessonCount { get; set; }

    public DateTime CreatedAt { get; set; }
}

public class RealLessonTransferWeekOption
{
    public int WeekNumber { get; set; }

    public DateTime WeekStartDate { get; set; }

    public DateTime WeekEndDate { get; set; }

    public DateTime ActiveStartDate { get; set; }

    public DateTime ActiveEndDate { get; set; }

    public WeekProperty RecommendedWeekProperty
    { get; set; }

    public string RecommendedWeekPropertyName
    { get; set; } = string.Empty;

    public bool IsTransferred { get; set; }

    public WeekProperty? TransferredWeekProperty
    { get; set; }

    public string? TransferredWeekPropertyName
    { get; set; }

    public int TransferredLessonCount { get; set; }
}
