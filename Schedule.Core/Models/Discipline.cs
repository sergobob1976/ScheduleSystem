using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Schedule.Core.Models;

public class Discipline
{
    public int Id { get; set; }

    public int SpecialtyId { get; set; }

    public string Name { get; set; } = string.Empty;

    public int? TotalHours { get; set; }

    public int? LectureHours { get; set; }

    public int? PracticalHours { get; set; }

    public int? LaboratoryHours { get; set; }

    public int? SeminarHours { get; set; }

    public int? OtherHours { get; set; }

    public Specialty? Specialty { get; set; }
}