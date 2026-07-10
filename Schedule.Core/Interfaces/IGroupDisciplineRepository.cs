using System;
using System.Collections.Generic;
using System.Text;
using Schedule.Core.Models;

namespace Schedule.Core.Interfaces;

public interface IGroupDisciplineRepository
{
    Task<IEnumerable<GroupDiscipline>> GetAllAsync();

    Task<IEnumerable<GroupDiscipline>> GetBySemesterIdAsync(
        int semesterId);

    Task<IEnumerable<GroupDiscipline>> GetByGroupIdAsync(
        int groupId);

    Task<IEnumerable<GroupDiscipline>> GetBySemesterAndGroupAsync(
        int semesterId,
        int groupId);

    Task<GroupDiscipline?> GetByIdAsync(int id);

    Task<GroupDiscipline?> GetBySemesterGroupAndDisciplineAsync(
        int semesterId,
        int groupId,
        int disciplineId);

    Task<int> CreateAsync(
        GroupDiscipline groupDiscipline);

    Task<bool> UpdateAsync(
        GroupDiscipline groupDiscipline);

    Task<bool> DeleteAsync(int id);
}