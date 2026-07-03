using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace Schedule.Maui.LocalModels
{
    [Table("ClassRooms")]
    public class ClassRoom
    {
        [PrimaryKey] // Вказуємо, що це головний ключ
        public string ClassRoomName { get; set; } = string.Empty;
    }
}
