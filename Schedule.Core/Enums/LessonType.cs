using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;

namespace Schedule.Core.Enums;

public enum LessonType
{
    [Description("Лекція")]
    Lecture = 1,

    [Description("Практичне")]
    Practical = 2,

    [Description("Лабораторне")]
    Laboratory = 3,

    [Description("Семінар")]
    Seminar = 4,

    [Description("Інше")]
    Other = 5
}
