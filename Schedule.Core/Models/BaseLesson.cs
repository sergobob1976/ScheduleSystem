using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Schedule.Core.Enums;

namespace Schedule.Core.Models;
public class BaseLesson
{
    public int Id { get; set; }

    public int GroupId { get; set; }
    public int TeacherId { get; set; }
    public int DisciplineId { get; set; }
    public int? ClassRoomId { get; set; }
    
    public int SemesterId { get; set; }

    public int LessonPosition { get; set; }      // Номер пари (1, 2, 3...)
    
    // Замість int ставимо наші Enums!
    public required WeekDay WeekDay { get; set; }             // День тижня (1 = Пн, 2 = Вт...)
    public required WeekProperty WeekProperty { get; set; }        // 1 = Чисельник, 2 = Знаменник, 0 = Кожний тиждень
    public required LessonType LessonType { get; set; }

    // Навігаційні властивості для зв'язків
    public Group? Group { get; set; }
    public Teacher? Teacher { get; set; }
    public Discipline? Discipline { get; set; }
    public ClassRoom? ClassRoom { get; set; }
    public Semester? Semester { get; set; }
}