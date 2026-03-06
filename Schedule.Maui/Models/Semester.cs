using SQLite;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Schedule.Maui.Models
{
    [SQLite.Table("Semesters")]
    public class Semester
    {
        [PrimaryKey]
        public string SemesterName { get; set; } = string.Empty;
    }
}
