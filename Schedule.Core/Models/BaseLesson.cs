using Schedule.Core.Enums;

namespace Schedule.Core.Models;

public class BaseLesson
{
    public int Id { get; set; }

    /// <summary>
    /// Конкретне затверджене призначення:
    /// викладач + група + дисципліна + семестр + вид заняття.
    ///
    /// Поки nullable для сумісності зі старими записами розкладу.
    /// </summary>
    public int? TeachingAssignmentId { get; set; }

    /*
     * Старі зовнішні ключі тимчасово залишаємо.
     * Після повного переведення розкладу на TeachingAssignment
     * їх можна буде прибрати.
     */
    public int GroupId { get; set; }

    public int TeacherId { get; set; }

    public int DisciplineId { get; set; }

    public int? ClassRoomId { get; set; }

    public int SemesterId { get; set; }

    /// <summary>
    /// Номер пари.
    /// </summary>
    public int LessonPosition { get; set; }

    public required WeekDay WeekDay { get; set; }

    public required WeekProperty WeekProperty { get; set; }

    public required LessonType LessonType { get; set; }

    public TeachingAssignment? TeachingAssignment { get; set; }

    public Group? Group { get; set; }

    public Teacher? Teacher { get; set; }

    public Discipline? Discipline { get; set; }

    public ClassRoom? ClassRoom { get; set; }

    public Semester? Semester { get; set; }
}