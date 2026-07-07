using System;
using System.Collections.Generic;
using System.Text;

namespace Schedule.Contracts.DTO;

public class UserDto
{
    public required string Username { get; set; }
    public required string Role { get; set; }
    public bool IsLoggedIn { get; set; }
    // Пароль передаємо лише при створенні/авторизації, 
    // але не виводимо в загальних списках, якщо це безпечно
    public required string Password { get; set; }
}
