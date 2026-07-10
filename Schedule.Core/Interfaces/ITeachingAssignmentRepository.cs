using System;
using System.Collections.Generic;
using System.Text;
using Schedule.Core.Enums;
using Schedule.Core.Models;

namespace Schedule.Core.Interfaces;

public interface ITeachingAssignmentRepository
{
    Task<IEnumerable<TeachingAssignment>> GetAllAsync();

    Task<IEnumerable<TeachingAssignment>>
        GetByGroupDisciplineIdAsync(int groupDisciplineId);

    Task<IEnumerable<TeachingAssignment>>
        GetByTeacherIdAsync(int teacherId);

    Task<TeachingAssignment?> GetByIdAsync(int id);

    Task<TeachingAssignment?> GetExistingAsync(
        int groupDisciplineId,
        int teacherId,
        LessonType lessonType);

    Task<int> GetAssignedHoursForGroupDisciplineTypeAsync(
        int groupDisciplineId,
        LessonType lessonType,
        int? excludedId = null);

    Task<int> GetAssignedHoursForTeacherDisciplineAsync(
        int teacherId,
        int semesterId,
        int disciplineId,
        int? excludedId = null);

    Task<int> CreateAsync(
        TeachingAssignment teachingAssignment);

    Task<bool> UpdateAsync(
        TeachingAssignment teachingAssignment);

    Task<bool> DeleteAsync(int id);
}