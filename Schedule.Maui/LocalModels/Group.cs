using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace Schedule.Maui.LocalModels
{
    [Table("Groups")]
    public class Group
    {
        [PrimaryKey]
        public string GroupName { get; set; } = string.Empty;
    }
}
