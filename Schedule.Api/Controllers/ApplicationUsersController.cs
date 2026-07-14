using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
