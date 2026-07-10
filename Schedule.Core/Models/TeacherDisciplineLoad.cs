using System;
using System.Collections.Generic;
using System.Text;

namespace Schedule.Core.Models;

public class TeacherDisciplineLoad
{
    public int Id { get; set; }

    public int TeacherSemesterLoadId { get; set; }

    public int DisciplineId { get; set; }

    /// <summary>
    /// Кількість годин, яку викладач повинен вичитати
    /// з цієї дисципліни протягом семестру.
    /// </summary>
    public int PlannedHours { get; set; }

    public TeacherSemesterLoad? TeacherSemesterLoad { get; set; }

    public Discipline? Discipline { get; set; }
}
