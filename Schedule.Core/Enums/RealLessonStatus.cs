using System.ComponentModel;

namespace Schedule.Core.Enums;

public enum RealLessonStatus
{
    [Description("Заплановано")]
    Planned = 0,

    [Description("Проведено")]
    Completed = 1,

    [Description("Скасовано")]
    Cancelled = 2
}
