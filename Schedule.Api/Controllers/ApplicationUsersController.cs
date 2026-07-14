using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using Schedule.Api.Services;
using Schedule.Core.DTOs;
using Schedule.Core.Interfaces;
using Schedule.Core.Models;

namespace Schedule.Api.Controllers;

[ApiController]
[Authorize(Roles = "Administrator")]
[Route("api/application-users")]
public class ApplicationUsersController : ControllerBase
{
    private static readonly Regex UserNamePattern = new(
        "^[A-Za-z0-9._-]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly IApplicationUserRepository _users;
    private readonly PasswordHashService _passwords;

    public ApplicationUsersController(
        IApplicationUserRepository users,
        PasswordHashService passwords)
    {
        _users = users;
        _passwords = passwords;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ApplicationUserSummaryResponse>>> GetAll()
    {
        var users = await _users.GetAllAsync();
        return Ok(users.Select(ToSummary));
    }

    [HttpPost]
    public async Task<ActionResult<ApplicationUserSummaryResponse>> CreateDispatcher(
        CreateDispatcherRequest request)
    {
        var userName = request.UserName.Trim();
        var displayName = request.DisplayName.Trim();

        if (userName.Length is < 3 or > 50 || !UserNamePattern.IsMatch(userName))
        {
            return BadRequest(new
            {
                Message = "Логін повинен містити від 3 до 50 латинських літер, цифр або символів . _ -"
            });
        }

        if (displayName.Length is < 2 or > 100)
            return BadRequest(new { Message = "Ім’я користувача повинно містити від 2 до 100 символів." });

        var validationMessage = _passwords.Validate(request.Password);
        if (validationMessage is not null)
            return BadRequest(new { Message = validationMessage });

        if (await _users.GetByUserNameAsync(userName) is not null)
            return Conflict(new { Message = $"Користувач із логіном «{userName}» уже існує." });

        var created = await _users.CreateAsync(new ApplicationUser
        {
            UserName = userName,
            DisplayName = displayName,
            PasswordHash = _passwords.Hash(request.Password),
            Role = "Dispatcher",
            IsActive = true
        });

        if (!created)
            return StatusCode(500, new { Message = "Не вдалося створити диспетчера." });

        var user = await _users.GetByUserNameAsync(userName);
        if (user is null)
            return StatusCode(500, new { Message = "Диспетчера створено, але не вдалося завантажити його дані." });

        return StatusCode(StatusCodes.Status201Created, ToSummary(user));
    }

    [HttpPatch("{id:int}/password")]
    public async Task<IActionResult> ChangePassword(
        int id,
        ChangeUserPasswordRequest request)
    {
        var validationMessage = _passwords.Validate(request.NewPassword);
        if (validationMessage is not null)
            return BadRequest(new { Message = validationMessage });

        var user = (await _users.GetAllAsync()).FirstOrDefault(item => item.Id == id);
        if (user is null)
            return NotFound(new { Message = $"Користувача з ID {id} не знайдено." });

        var updated = await _users.UpdatePasswordHashAsync(id, _passwords.Hash(request.NewPassword));
        if (!updated)
            return NotFound(new { Message = $"Користувача з ID {id} не знайдено." });

        return NoContent();
    }

    private static ApplicationUserSummaryResponse ToSummary(ApplicationUser user) => new()
    {
        Id = user.Id,
        UserName = user.UserName,
        DisplayName = user.DisplayName,
        Role = user.Role,
        IsActive = user.IsActive
    };
}
