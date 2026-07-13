namespace Schedule.Core.DTOs;

public class LoginRequest
{
    public string UserName { get; set; } = "";
    public string Password { get; set; } = "";
}

public class AuthenticatedUserResponse
{
    public int Id { get; set; }
    public required string UserName { get; set; }
    public required string DisplayName { get; set; }
    public required string Role { get; set; }
}
