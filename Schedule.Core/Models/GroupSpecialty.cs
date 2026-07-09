using System;
using System.Collections.Generic;
using System.Text;

namespace Schedule.Core.Models;

public class GroupSpecialty
{
    public int Id { get; set; }

    public int GroupId { get; set; }

    public int SpecialtyId { get; set; }

    public Group? Group { get; set; }

    public Specialty? Specialty { get; set; }
}
