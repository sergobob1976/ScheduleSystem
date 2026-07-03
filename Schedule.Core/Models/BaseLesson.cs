using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Schedule.Core.Models
{
    public class BaseLesson
    {
        public int RealLesson_id { get; set; }

        public string? Semester { get; set; }

        public string? WeekDay { get; set; }

        public string? Group { get; set; }

        public int LessonPosition { get; set; }

        public string? Discipline { get; set; }

        public string? Teacher { get; set; }

        public string? WeekProperty { get; set; }

        public string? ClassRoom { get; set; }
    }
}

