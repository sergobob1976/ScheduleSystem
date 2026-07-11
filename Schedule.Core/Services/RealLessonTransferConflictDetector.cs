using Schedule.Core.Models;

namespace Schedule.Core.Services;

public static class RealLessonTransferConflictDetector
{
    public static string? FindInternalConflict(
        IReadOnlyCollection<RealLesson> lessons)
    {
        if (HasDuplicates(
                lessons,
                lesson => new
                {
                    lesson.GroupId,
                    Date = lesson.LessonDate.Date,
                    lesson.LessonPosition
                }))
        {
            return "У базовому розкладі виявлено " +
                   "конфлікт занять групи.";
        }

        if (HasDuplicates(
                lessons,
                lesson => new
                {
                    lesson.TeacherId,
                    Date = lesson.LessonDate.Date,
                    lesson.LessonPosition
                }))
        {
            return "У базовому розкладі виявлено " +
                   "конфлікт занять викладача.";
        }

        if (HasDuplicates(
                lessons.Where(
                    lesson =>
                        lesson.ClassRoomId.HasValue),
                lesson => new
                {
                    lesson.ClassRoomId,
                    Date = lesson.LessonDate.Date,
                    lesson.LessonPosition
                }))
        {
            return "У базовому розкладі виявлено " +
                   "конфлікт використання аудиторії.";
        }

        return null;
    }

    public static RealLesson? FindExistingConflict(
        IReadOnlyCollection<RealLesson> newLessons,
        IReadOnlyCollection<RealLesson> existingLessons)
    {
        return newLessons
            .Select(
                newLesson =>
                    existingLessons.FirstOrDefault(
                        existing =>
                            existing.LessonDate.Date ==
                            newLesson.LessonDate.Date &&
                            existing.LessonPosition ==
                            newLesson.LessonPosition &&
                            (
                                existing.GroupId ==
                                newLesson.GroupId ||
                                existing.TeacherId ==
                                newLesson.TeacherId ||
                                (
                                    newLesson.ClassRoomId
                                        .HasValue &&
                                    existing.ClassRoomId ==
                                    newLesson.ClassRoomId
                                )
                            )))
            .FirstOrDefault(conflict => conflict != null);
    }

    private static bool HasDuplicates<TKey>(
        IEnumerable<RealLesson> lessons,
        Func<RealLesson, TKey> keySelector)
        where TKey : notnull
    {
        return lessons
            .GroupBy(keySelector)
            .Any(group => group.Count() > 1);
    }
}
