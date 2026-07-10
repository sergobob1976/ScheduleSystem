using System;
using System.Collections.Generic;
using System.Text;

namespace Schedule.Core.Models;

public class GroupDiscipline
{
    public int Id { get; set; }

    public int SemesterId { get; set; }

    public int GroupId { get; set; }

    public int DisciplineId { get; set; }

    /// <summary>
    /// Планова кількість лекційних годин для групи.
    /// </summary>
    public int LectureHours { get; set; }

    /// <summary>
    /// Планова кількість практичних годин для групи.
    /// </summary>
    public int PracticalHours { get; set; }

    /// <summary>
    /// Планова кількість лабораторних годин для групи.
    /// </summary>
    public int LaboratoryHours { get; set; }

    /// <summary>
    /// Планова кількість семінарських годин для групи.
    /// </summary>
    public int SeminarHours { get; set; }

    /// <summary>
    /// Інші види навчального навантаження.
    /// </summary>
    public int OtherHours { get; set; }

    public int TotalHours =>
        LectureHours +
        PracticalHours +
        LaboratoryHours +
        SeminarHours +
        OtherHours;

    public Semester? Semester { get; set; }

    public Group? Group { get; set; }

    public Discipline? Discipline { get; set; }

    public List<TeachingAssignment> TeachingAssignments { get; set; } = [];
}