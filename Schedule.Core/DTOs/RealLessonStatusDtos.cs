using Schedule.Core.Enums;

namespace Schedule.Core.DTOs;

public class UpdateRealLessonStatusRequest
{
    public RealLessonStatus Status { get; set; }
}

public class UpdateRealLessonStatusResponse
{
    public int LessonId { get; set; }

    public RealLessonStatus Status { get; set; }

    public string StatusName { get; set; } =
        string.Empty;

    public string Message { get; set; } =
        string.Empty;
}
