using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace Schedule.Maui.LocalModels
{
    [Table("LessonPositions")]
    public class LessonPosition
    {
        [PrimaryKey]
        public int LessonPositionNumber { get; set; }
    }
}
