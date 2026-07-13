using System;
using System.Collections.Generic;
using System.Text;
using Schedule.Core.Models;

namespace Schedule.Core.Interfaces;

public interface ITeacherRepository
{
    Task<IEnumerable<Teacher>> GetAllAsync();
    Task<Teacher?> GetByIdAsync(int id);
    Task<Teacher?> GetByEmailAsync(string email);
    Task<int> CreateAsync(Teacher teacher);
    Task<bool> UpdateAsync(Teacher teacher);
    Task<bool> DeleteAsync(int id);
}
