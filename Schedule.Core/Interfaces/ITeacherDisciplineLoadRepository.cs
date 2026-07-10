using System;
using System.Collections.Generic;
using System.Text;
using Schedule.Core.Models;

namespace Schedule.Core.Interfaces;

public interface ITeacherDisciplineLoadRepository
{
    Task<IEnumerable<TeacherDisciplineLoad>> GetAllAsync();

    Task<IEnumerable<TeacherDisciplineLoad>>
        GetByTeacherSemesterLoadIdAsync(int teacherSemesterLoadId);

    Task<TeacherDisciplineLoad?> GetByIdAsync(int id);

    Task<TeacherDisciplineLoad?> GetByLoadAndDisciplineAsync(
        int teacherSemesterLoadId,
        int disciplineId);

    Task<int> GetTotalPlannedHoursAsync(
        int teacherSemesterLoadId,
        int? excludedId = null);

    Task<int> CreateAsync(
        TeacherDisciplineLoad teacherDisciplineLoad);

    Task<bool> UpdateAsync(
        TeacherDisciplineLoad teacherDisciplineLoad);

    Task<bool> DeleteAsync(int id);
}
