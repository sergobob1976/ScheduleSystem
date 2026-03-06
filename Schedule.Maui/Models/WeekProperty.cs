using SQLite;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Schedule.Maui.Models
{
    [SQLite.Table("WeekProperties")]
    public class WeekProperty
    {
        [PrimaryKey]
        public string WeekPropertyName { get; set; } = string.Empty;
    }
}
