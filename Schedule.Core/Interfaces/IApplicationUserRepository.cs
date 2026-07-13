using Schedule.Core.Models;

namespace Schedule.Core.Interfaces;

public interface IApplicationUserRepository
{
    Task<ApplicationUser?> GetByUserNameAsync(string userName);
}
