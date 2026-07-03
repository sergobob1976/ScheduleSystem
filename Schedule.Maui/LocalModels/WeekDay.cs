using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace Schedule.Maui.LocalModels
{
    [Table("WeekDays")]
    public class WeekDay
    {
        [PrimaryKey]
        public string WeekDayName { get; set; } = string.Empty;
    }
}
