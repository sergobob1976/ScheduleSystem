using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;

namespace Schedule.Core.Enums;

public enum WeekProperty
{
    [Description("Кожний тиждень")]
    EveryWeek = 0,

    [Description("Чисельник")]
    Numerator = 1,

    [Description("Знаменник")]
    Denominator = 2
}
