using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Schedule.Core.Models
{
    public class WeekDay
    {
        public string WeekDayName { get; set; } = string.Empty;
    }
}
