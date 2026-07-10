using System;
using System.Collections.Generic;
using System.Text;
using Schedule.Core.Models;

namespace Schedule.Core.Interfaces;

public interface IGroupSpecialtyRepository
{
    Task<IEnumerable<GroupSpecialty>> GetAllAsync();

    Task<IEnumerable<GroupSpecialty>> GetByGroupIdAsync(
        int groupId);

    Task<IEnumerable<GroupSpecialty>> GetBySpecialtyIdAsync(
        int specialtyId);

    Task<GroupSpecialty?> GetByIdAsync(int id);

    Task<GroupSpecialty?> GetExistingAsync(
        int groupId,
        int specialtyId);

    Task<int> CreateAsync(
        GroupSpecialty groupSpecialty);

    Task<bool> DeleteAsync(int id);
}
