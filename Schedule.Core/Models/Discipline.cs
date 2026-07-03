using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Schedule.Core.Models
{
    public class Discipline
    {
        public string DisciplineName { get; set; } = string.Empty;

        public int TotalHours { get; set; }
    }
}
