using SQLite;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Schedule.Maui.Models
{
    [SQLite.Table("WeekDays")]
    public class WeekDay
    {
        [PrimaryKey]
        public string WeekDayName { get; set; } = string.Empty;
    }
}
