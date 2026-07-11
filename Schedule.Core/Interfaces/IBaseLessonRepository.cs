using Schedule.Core.Models;

namespace Schedule.Core.Interfaces;

public interface IBaseLessonRepository
{
    Task<IEnumerable<BaseLesson>> GetAllAsync();

    Task<BaseLesson?> GetByIdAsync(int id);

    Task<IEnumerable<BaseLesson>> GetByGroupIdAsync(
        int groupId);

    Task<IEnumerable<BaseLesson>> GetConflictingLessonsAsync(
        BaseLesson lesson,
        int? excludedId = null);

    Task<int> CreateAsync(BaseLesson lesson);

    Task<bool> UpdateAsync(BaseLesson lesson);

    Task<bool> DeleteAsync(int id);
}