using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Schedule.Core.Models;

public class RealLesson
{
    public int Id { get; set; }

    // Зовнішні ключі для бази даних
    public int GroupId { get; set; }
    public int TeacherId { get; set; }
    public int DisciplineId { get; set; }
    public int? ClassRoomId { get; set; } // Може бути NULL, якщо пара суто дистанційна
    public int SemesterId { get; set; }

    // Основні параметри пари
    public DateTime LessonDate { get; set; }     // Дата (наприклад, 2026-09-15)
    public int LessonPosition { get; set; }      // Номер пари (1, 2, 3...)
    public int WeekDay { get; set; }             // День тижня (1 = Пн, 2 = Вт...)
    public int WeekProperty { get; set; }        // 1 = Чисельник, 2 = Знаменник, 0 = Кожен тиждень

    // Ті самі поля для лінків, які викладачі зможуть редагувати
    public string? ConferenceLink { get; set; }  // Посилання на відеоконференцію
    public string? ResourceLink { get; set; }    // Посилання на матеріали

    // Навігаційні властивості (для красивого виводу JSON в MAUI)
    public Group? Group { get; set; }
    public Teacher? Teacher { get; set; }
    public Discipline? Discipline { get; set; }
    public ClassRoom? ClassRoom { get; set; }
    public Semester? Semester { get; set; }
}