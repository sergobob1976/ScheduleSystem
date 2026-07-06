using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Schedule.Core.Models;

public class Semester
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; // Наприклад, "Осінній 2026"
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}
