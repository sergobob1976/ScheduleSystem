using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Schedule.Core.Models;
using Schedule.Core.DTOs;
using Schedule.Core.Enums;

namespace Schedule.Core.Interfaces;

public interface IRealLessonRepository
{
    Task<IEnumerable<RealLesson>> GetAllAsync();
    Task<RealLesson?> GetByIdAsync(int id);

    // Спеціальні методи для MAUI-додатку
    Task<IEnumerable<RealLesson>> GetByGroupIdAsync(int groupId);
    Task<IEnumerable<RealLesson>> GetByTeacherIdAsync(int teacherId);

    Task<IEnumerable<RealLesson>>
        GetBySemesterAndDateRangeAsync(
            int semesterId,
            DateTime startDate,
            DateTime endDate,
            int? groupId = null);

    Task<IEnumerable<RealLesson>>
        GetConflictingLessonsAsync(
            RealLesson lesson,
            int? excludedId = null);

    Task<int> CreateAsync(RealLesson lesson);
    Task<bool> UpdateAsync(RealLesson lesson);

    // Окремий швидкий метод суто для викладачів, щоб оновлювати ЛІНКИ
    Task<bool> UpdateLinksAsync(int lessonId, string? conferenceLink, string? resourceLink);

    Task<bool> UpdateStatusAsync(
        int lessonId,
        RealLessonStatus status);

    Task<bool> DeleteAsync(int id);

    Task<TransferRealLessonWeekResult> TransferWeekAsync(
        int semesterId,
        DateTime weekStartDate,
        DateTime weekEndDate,
        WeekProperty weekProperty,
        IReadOnlyCollection<RealLesson> lessons);

    Task<IEnumerable<TransferredRealLessonWeekItem>>
        GetTransferredWeeksAsync(int semesterId);
}
