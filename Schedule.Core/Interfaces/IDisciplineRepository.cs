using System;
using System.Collections.Generic;
using System.Text;
using Schedule.Core.Models;

namespace Schedule.Core.Interfaces;

public interface IDisciplineRepository
{
    Task<IEnumerable<Discipline>> GetAllAsync();
    Task<Discipline?> GetByIdAsync(int id);
    Task<int> CreateAsync(Discipline discipline);
    Task<bool> UpdateAsync(Discipline discipline);
    Task<bool> DeleteAsync(int id);
}
