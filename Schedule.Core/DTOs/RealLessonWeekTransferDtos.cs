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
