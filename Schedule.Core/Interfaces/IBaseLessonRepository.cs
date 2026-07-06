using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using Schedule.Core.Models;

namespace Schedule.Core.Interfaces;

public interface IBaseLessonRepository
{
    Task<IEnumerable<BaseLesson>> GetAllAsync();
    Task<BaseLesson?> GetByIdAsync(int id);
    Task<IEnumerable<BaseLesson>> GetByGroupIdAsync(int groupId);
    Task<int> CreateAsync(BaseLesson lesson);
    Task<bool> UpdateAsync(BaseLesson lesson);
    Task<bool> DeleteAsync(int id);
}
