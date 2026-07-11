using Schedule.Core.Enums;
using Schedule.Core.Models;

namespace Schedule.Core.Services;

public static class SemesterCalendar
{
    public static DateTime GetWeekStart(DateTime date)
    {
        int daysFromMonday =
            ((int)date.DayOfWeek + 6) % 7;

        return date.Date.AddDays(-daysFromMonday);
    }

    public static IEnumerable<SemesterCalendarWeek>
        GetWeeks(Semester semester)
    {
        ValidateSemester(semester);

        DateTime firstWeekStart =
            GetWeekStart(semester.StartDate);

        DateTime lastWeekStart =
            GetWeekStart(semester.EndDate);

        int weekNumber = 1;

        for (DateTime weekStart = firstWeekStart;
             weekStart <= lastWeekStart;
             weekStart = weekStart.AddDays(7))
        {
            yield return new SemesterCalendarWeek
            {
                WeekNumber = weekNumber,
                WeekStartDate = weekStart,
                WeekEndDate = weekStart.AddDays(6),
                WeekProperty = GetWeekProperty(
                    semester.FirstWeekProperty,
                    weekNumber)
            };

            weekNumber++;
        }
    }

    public static DateTime GetLessonDate(
        DateTime weekStartDate,
        WeekDay weekDay)
    {
        if (!Enum.IsDefined(weekDay))
        {
            throw new ArgumentOutOfRangeException(
                nameof(weekDay),
                weekDay,
                "Невідомий день тижня.");
        }

        return GetWeekStart(weekStartDate)
            .AddDays((int)weekDay - 1);
    }

    public static bool IsLessonIncluded(
        WeekProperty lessonWeekProperty,
        WeekProperty calendarWeekProperty)
    {
        return lessonWeekProperty ==
                   WeekProperty.EveryWeek ||
               lessonWeekProperty ==
               calendarWeekProperty;
    }

    public static int CountOccurrences(
        Semester semester,
        WeekDay weekDay,
        WeekProperty weekProperty)
    {
        int count = 0;

        foreach (var calendarWeek in GetWeeks(semester))
        {
            if (!IsLessonIncluded(
                    weekProperty,
                    calendarWeek.WeekProperty))
            {
                continue;
            }

            DateTime lessonDate = GetLessonDate(
                calendarWeek.WeekStartDate,
                weekDay);

            if (lessonDate >= semester.StartDate.Date &&
                lessonDate <= semester.EndDate.Date)
            {
                count++;
            }
        }

        return count;
    }

    public static int CountOccurrences(
        Semester semester,
        BaseLesson lesson)
    {
        return CountOccurrences(
            semester,
            lesson.WeekDay,
            lesson.WeekProperty);
    }

    private static WeekProperty GetWeekProperty(
        WeekProperty firstWeekProperty,
        int weekNumber)
    {
        bool isFirstType = weekNumber % 2 == 1;

        if (isFirstType)
        {
            return firstWeekProperty;
        }

        return firstWeekProperty ==
               WeekProperty.Numerator
            ? WeekProperty.Denominator
            : WeekProperty.Numerator;
    }

    private static void ValidateSemester(
        Semester semester)
    {
        if (semester.EndDate.Date <
            semester.StartDate.Date)
        {
            throw new ArgumentException(
                "Дата завершення семестру не може " +
                "бути раніше дати початку.",
                nameof(semester));
        }

        if (semester.FirstWeekProperty is not
            (WeekProperty.Numerator or
             WeekProperty.Denominator))
        {
            throw new ArgumentException(
                "Перший тиждень семестру має бути " +
                "чисельником або знаменником.",
                nameof(semester));
        }
    }
}

public class SemesterCalendarWeek
{
    public int WeekNumber { get; set; }

    public DateTime WeekStartDate { get; set; }

    public DateTime WeekEndDate { get; set; }

    public WeekProperty WeekProperty { get; set; }
}
