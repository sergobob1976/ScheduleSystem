using SQLite;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Schedule.Maui.Models
{
    [SQLite.Table("LessonPositions")]
    public class LessonPosition
    {
        [PrimaryKey]
        public int LessonPositionNumber { get; set; }
    }
}
