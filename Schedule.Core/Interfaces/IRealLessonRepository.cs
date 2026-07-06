using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Schedule.Core.Models;

namespace Schedule.Core.Interfaces;

public interface IRealLessonRepository
{
    Task<IEnumerable<RealLesson>> GetAllAsync();
    Task<RealLesson?> GetByIdAsync(int id);

    // Спеціальні методи для MAUI-додатку
    Task<IEnumerable<RealLesson>> GetByGroupIdAsync(int groupId);
    Task<IEnumerable<RealLesson>> GetByTeacherIdAsync(int teacherId);

    Task<int> CreateAsync(RealLesson lesson);
    Task<bool> UpdateAsync(RealLesson lesson);

    // Окремий швидкий метод суто для викладачів, щоб оновлювати ЛІНКИ
    Task<bool> UpdateLinksAsync(int lessonId, string? conferenceLink, string? resourceLink);

    Task<bool> DeleteAsync(int id);
}
