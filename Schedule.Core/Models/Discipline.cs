using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Schedule.Core.Models
{
    public class Discipline
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? TotalHours { get; set; } // Кількість годин може бути не вказана (NULL)
    }
}
