using System;
using System.Collections.Generic;
using System.Text;
using Schedule.Core.Models;

namespace Schedule.Core.Interfaces;

public interface ISemesterRepository
{
    Task<IEnumerable<Semester>> GetAllAsync();
    Task<Semester?> GetByIdAsync(int id);
    Task<int> CreateAsync(Semester semester);
    Task<bool> UpdateAsync(Semester semester);
    Task<bool> DeleteAsync(int id);
}
