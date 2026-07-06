using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;

namespace Schedule.Core.Enums;

public enum WeekDay
{
    [Description("Понеділок")]
    Monday = 1,

    [Description("Вівторок")]
    Tuesday = 2,

    [Description("Середа")]
    Wednesday = 3,

    [Description("Четвер")]
    Thursday = 4,

    [Description("П'ятниця")]
    Friday = 5,

    [Description("Субота")]
    Saturday = 6,

    [Description("Неділя")]
    Sunday = 7
}
