using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schedule.Api.Services;
using Schedule.Core.DTOs;
using Schedule.Core.Interfaces;
using Schedule.Core.Models;

namespace Schedule.Api.Controllers;

[ApiController]
[Authorize(Roles = "Administrator,Dispatcher,Teacher")]
[Route("api/authentication")]
public class AuthenticationController : ControllerBase
{
    private const string DummyPasswordHash =
        "pbkdf2-sha256.210000.scHGbBUZaNDhwhQNfpjcCQ==.2hTjL7mZ+D5PV+kh5lJBOfwZkovddVRNDpSqUCsaHbk=";

    private readonly IApplicationUserRepository _users;
    private readonly PasswordHashService _passwords;

    public AuthenticationController(
        IApplicationUserRepository users,
        PasswordHashService passwords)
    {
        _users = users;
        _passwords = passwords;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthenticatedUserResponse>> Login(LoginRequest request)
    {
        var userName = request.UserName.Trim().ToLowerInvariant();
        var user = string.IsNullOrWhiteSpace(userName)
            ? null
            : await _users.GetByUserNameAsync(userName);

        var passwordHash = user?.PasswordHash ?? DummyPasswordHash;
        var passwordIsValid = _passwords.Verify(request.Password, passwordHash);

        if (user is null || !user.IsActive || !passwordIsValid)
        {
            return Unauthorized(new { Message = "Неправильний логін або пароль." });
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.GivenName, user.DisplayName),
            new(ClaimTypes.Role, user.Role)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme)));

        return Ok(ToResponse(user));
    }

    [HttpGet("me")]
    public ActionResult<AuthenticatedUserResponse> Me()
    {
        return Ok(new AuthenticatedUserResponse
        {
            Id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
            UserName = User.Identity!.Name!,
            DisplayName = User.FindFirstValue(ClaimTypes.GivenName)!,
            Role = User.FindFirstValue(ClaimTypes.Role)!
        });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    private static AuthenticatedUserResponse ToResponse(ApplicationUser user) => new()
    {
        Id = user.Id,
        UserName = user.UserName,
        DisplayName = user.DisplayName,
        Role = user.Role
    };
}
