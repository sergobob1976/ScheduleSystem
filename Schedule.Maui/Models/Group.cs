using SQLite;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Schedule.Maui.Models
{
    [SQLite.Table("Groups")]
    public class Group
    {
        [PrimaryKey]
        public string GroupName { get; set; } = string.Empty;
    }
}
