using System;
using System.Collections.Generic;
using System.Text;

namespace Schedule.Core.Models;

public class Specialty
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty; // F7
    public string Name { get; set; } = string.Empty;
}
