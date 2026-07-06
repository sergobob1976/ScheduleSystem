using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Schedule.Core.Models
{
    public class ClassRoom
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
