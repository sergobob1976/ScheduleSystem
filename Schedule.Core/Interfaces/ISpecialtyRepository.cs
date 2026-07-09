using System;
using System.Collections.Generic;
using System.Text;
using Schedule.Core.Models;

namespace Schedule.Core.Interfaces;

public interface ISpecialtyRepository
{
    Task<IEnumerable<Specialty>> GetAllAsync();
    Task<Specialty?> GetByIdAsync(int id);
    Task<int> CreateAsync(Specialty specialty);
    Task<bool> UpdateAsync(Specialty specialty);
    Task<bool> DeleteAsync(int id);
}