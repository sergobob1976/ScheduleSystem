using Schedule.Core.Models; // Твоя модель Group має бути тут

namespace Schedule.Core.Interfaces;

public interface IGroupRepository
{
    Task<IEnumerable<Group>> GetAllAsync();
    Task<Group?> GetByIdAsync(int id);
    Task<int> CreateAsync(Group group);
    Task<bool> UpdateAsync(Group group);
    Task<bool> DeleteAsync(int id);
}
