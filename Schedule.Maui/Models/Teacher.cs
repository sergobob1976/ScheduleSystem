using SQLite;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Schedule.Maui.Models
{
    [SQLite.Table("Teachers")]
    public class Teacher
    {
        [PrimaryKey]
        public string TeacherName { get; set; } = string.Empty;

        public string? Position { get; set; } // Може бути NULL, якщо посада не вказана
    }
}
