using System;
using System.Collections.Generic;
using System.Text;

namespace Schedule.Core.Models;

public class TeacherSemesterLoad
{
    public int Id { get; set; }

    public int TeacherId { get; set; }

    public int SemesterId { get; set; }

    /// <summary>
    /// Загальне планове навантаження викладача на семестр.
    /// </summary>
    public int PlannedHours { get; set; }

    public Teacher? Teacher { get; set; }

    public Semester? Semester { get; set; }

    /// <summary>
    /// Планове навантаження викладача за окремими дисциплінами.
    /// </summary>
    public List<TeacherDisciplineLoad> DisciplineLoads { get; set; } = [];
}