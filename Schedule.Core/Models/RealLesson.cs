using Schedule.Core.Enums;

namespace Schedule.Core.Models;

public class RealLesson
{
    public int Id { get; set; }

    /// <summary>
    /// Конкретне затверджене призначення:
    /// викладач + група + дисципліна + семестр + вид заняття.
    ///
    /// Поки nullable для сумісності зі старими записами.
    /// </summary>
    public int? TeachingAssignmentId { get; set; }

    /*
     * Старі зовнішні ключі тимчасово залишаємо.
     * Вони потрібні, поки репозиторії та UI розкладу
     * повністю не переведені на TeachingAssignment.
     */
    public int GroupId { get; set; }

    public int TeacherId { get; set; }

    public int DisciplineId { get; set; }

    public int? ClassRoomId { get; set; }

    public int SemesterId { get; set; }

    /// <summary>
    /// Фактична дата проведення заняття.
    /// </summary>
    public DateTime LessonDate { get; set; }

    /// <summary>
    /// Номер пари.
    /// </summary>
    public int LessonPosition { get; set; }

    public required WeekDay WeekDay { get; set; }

    public required WeekProperty WeekProperty { get; set; }

    public required LessonType LessonType { get; set; }

    public string? ConferenceLink { get; set; }

    public string? ResourceLink { get; set; }

    public TeachingAssignment? TeachingAssignment { get; set; }

    public Group? Group { get; set; }

    public Teacher? Teacher { get; set; }

    public Discipline? Discipline { get; set; }

    public ClassRoom? ClassRoom { get; set; }

    public Semester? Semester { get; set; }
}