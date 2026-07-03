using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace Schedule.Maui.LocalModels
{
    [Table("Semesters")]
    public class Semester
    {
        [PrimaryKey]
        public string SemesterName { get; set; } = string.Empty;
    }
}
