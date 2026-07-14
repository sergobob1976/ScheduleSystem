using Schedule.Core.Models;

namespace Schedule.Core.Interfaces;

public interface IApplicationUserRepository
{
    Task<IEnumerable<ApplicationUser>> GetAllAsync();
    Task<ApplicationUser?> GetByUserNameAsync(string userName);
    Task<bool> CreateAsync(ApplicationUser user);
    Task<bool> UpdatePasswordHashAsync(int id, string passwordHash);
    Task<bool> DeleteDispatcherAsync(int id);
}
