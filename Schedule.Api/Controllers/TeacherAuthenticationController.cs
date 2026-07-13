using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schedule.Api.Authentication;

namespace Schedule.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/teacher-authentication")]
public class TeacherAuthenticationController : ControllerBase
{
    private readonly GoogleAuthenticationSettings _settings;

    public TeacherAuthenticationController(GoogleAuthenticationSettings settings)
    {
        _settings = settings;
    }

    [HttpGet("google-login")]
    public IActionResult GoogleLogin()
    {
        if (!_settings.IsConfigured)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                Message = "Вхід через Google ще не налаштовано адміністратором системи."
            });
        }

        return Challenge(
            new AuthenticationProperties
            {
                RedirectUri = $"{_settings.WebBaseUrl.TrimEnd('/')}/teacher-schedule"
            },
            GoogleDefaults.AuthenticationScheme);
    }
}
