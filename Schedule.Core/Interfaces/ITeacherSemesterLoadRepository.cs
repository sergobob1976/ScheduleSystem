using System;
using System.Collections.Generic;
using System.Text;
using Schedule.Core.Models;

namespace Schedule.Core.Interfaces;

public interface ITeacherSemesterLoadRepository
{
    Task<IEnumerable<TeacherSemesterLoad>> GetAllAsync();

    Task<IEnumerable<TeacherSemesterLoad>> GetBySemesterIdAsync(int semesterId);

    Task<IEnumerable<TeacherSemesterLoad>> GetByTeacherIdAsync(int teacherId);

    Task<TeacherSemesterLoad?> GetByIdAsync(int id);

    Task<TeacherSemesterLoad?> GetByTeacherAndSemesterAsync(
        int teacherId,
        int semesterId);

    Task<int> CreateAsync(TeacherSemesterLoad teacherSemesterLoad);

    Task<bool> UpdateAsync(TeacherSemesterLoad teacherSemesterLoad);

    Task<bool> DeleteAsync(int id);
}
