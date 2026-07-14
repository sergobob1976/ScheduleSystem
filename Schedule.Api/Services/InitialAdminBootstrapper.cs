using Schedule.Core.Interfaces;
using Schedule.Core.Models;

namespace Schedule.Api.Services;

public class InitialAdminBootstrapper
{
    private readonly IConfiguration _configuration;
    private readonly IApplicationUserRepository _users;
    private readonly PasswordHashService _passwords;
    private readonly ILogger<InitialAdminBootstrapper> _logger;

    public InitialAdminBootstrapper(
        IConfiguration configuration,
        IApplicationUserRepository users,
        PasswordHashService passwords,
        ILogger<InitialAdminBootstrapper> logger)
    {
        _configuration = configuration;
        _users = users;
        _passwords = passwords;
        _logger = logger;
    }

    public async Task EnsureCreatedAsync()
    {
        var users = await _users.GetAllAsync();
        if (users.Any(user => user.Role == "Administrator"))
        {
            return;
        }

        var userName = _configuration["BootstrapAdmin:UserName"]?.Trim();
        var displayName = _configuration["BootstrapAdmin:DisplayName"]?.Trim();
        var password = _configuration["BootstrapAdmin:Password"];

        if (string.IsNullOrWhiteSpace(userName))
        {
            userName = "admin";
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = "Адміністратор";
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "У базі даних немає адміністратора. Задайте змінну середовища " +
                "BootstrapAdmin__Password для створення першого адміністратора.");
        }

        var validationMessage = _passwords.Validate(password);
        if (validationMessage is not null)
        {
            throw new InvalidOperationException(
                $"Пароль першого адміністратора не відповідає вимогам: {validationMessage}");
        }

        var created = await _users.CreateAsync(new ApplicationUser
        {
            UserName = userName,
            DisplayName = displayName,
            PasswordHash = _passwords.Hash(password),
            Role = "Administrator",
            IsActive = true
        });

        if (!created)
        {
            throw new InvalidOperationException(
                "Не вдалося створити першого адміністратора системи.");
        }

        _logger.LogInformation(
            "Створено першого адміністратора системи з іменем користувача {UserName}.",
            userName);
    }
}
