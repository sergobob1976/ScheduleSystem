using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace Schedule.Maui.LocalModels
{
    [Table("WeekProperties")]
    public class WeekProperty
    {
        [PrimaryKey]
        public string WeekPropertyName { get; set; } = string.Empty;
    }
}
