using System;
using System.Collections.Generic;
using System.Text;

namespace Schedule.Core.Models;

public class TeacherDiscipline
{
    public int Id { get; set; }

    public int TeacherId { get; set; }
    public int DisciplineId { get; set; }

    public Teacher? Teacher { get; set; }
    public Discipline? Discipline { get; set; }
}
