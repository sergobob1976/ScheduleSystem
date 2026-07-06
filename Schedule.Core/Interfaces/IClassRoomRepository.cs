using System;
using System.Collections.Generic;
using System.Text;
using Schedule.Core.Models;

namespace Schedule.Core.Interfaces;

public interface IClassRoomRepository
{
    Task<IEnumerable<ClassRoom>> GetAllAsync();
    Task<ClassRoom?> GetByIdAsync(int id);
    Task<int> CreateAsync(ClassRoom classRoom);
    Task<bool> UpdateAsync(ClassRoom classRoom);
    Task<bool> DeleteAsync(int id);
}
