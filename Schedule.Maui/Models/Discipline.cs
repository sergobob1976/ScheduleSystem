using SQLite;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Schedule.Maui.Models
{
    [SQLite.Table("Disciplines")]
    public class Discipline
    {
        [PrimaryKey]
        public string DisciplineName { get; set; } = string.Empty;

        public int TotalHours { get; set; }
    }
}
