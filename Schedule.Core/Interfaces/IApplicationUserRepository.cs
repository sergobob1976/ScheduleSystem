using Schedule.Core.Models;

namespace Schedule.Core.Interfaces;

public interface IApplicationUserRepository
{
    Task<IEnumerable<ApplicationUser>> GetAllAsync();
    Task<ApplicationUser?> GetByUserNameAsync(string userName);
    Task<bool> UpdatePasswordHashAsync(int id, string passwordHash);
}
