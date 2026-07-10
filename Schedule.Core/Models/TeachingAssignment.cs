using System;
using System.Collections.Generic;
using System.Text;

using Schedule.Core.Enums;

namespace Schedule.Core.Models;

public class TeachingAssignment
{
    public int Id { get; set; }

    /// <summary>
    /// Дисципліна конкретної групи в конкретному семестрі.
    /// </summary>
    public int GroupDisciplineId { get; set; }

    public int TeacherId { get; set; }

    /// <summary>
    /// Вид занять, який проводить викладач:
    /// лекція, практика, лабораторна тощо.
    /// </summary>
    public LessonType LessonType { get; set; }

    /// <summary>
    /// Кількість годин цього виду занять,
    /// призначена викладачу для конкретної групи.
    /// </summary>
    public int AssignedHours { get; set; }

    public GroupDiscipline? GroupDiscipline { get; set; }

    public Teacher? Teacher { get; set; }
}
