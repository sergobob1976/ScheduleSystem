using System;
using System.Collections.Generic;
using System.Text;

namespace Schedule.Maui.Models
{
    public enum UserRole { Supervisor, Admin, Teacher, Student, Guest }

    public class User
    {
        public string Username { get; set; } = string.Empty;
        public string Message { get; set; } = "Вітаємо у розкладі!";
        public UserRole Role { get; set; } = UserRole.Guest;
        public bool IsLoggedIn { get; set; } = false;
    }
}

