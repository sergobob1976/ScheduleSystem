using System.Security.Cryptography;

namespace Schedule.Api.Services;

public class PasswordHashService
{
    private const int Iterations = 210_000;

    public string? Validate(string password)
    {
        if (string.IsNullOrEmpty(password))
            return "Пароль не може бути порожнім.";
        if (password.Length < 12)
            return "Пароль повинен містити щонайменше 12 символів.";
        if (password.Length > 128)
            return "Пароль не може містити більше 128 символів.";
        if (!password.Any(char.IsLetter) || !password.Any(char.IsDigit))
            return "Пароль повинен містити хоча б одну літеру та одну цифру.";
        return null;
    }

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            32);

        return $"pbkdf2-sha256.{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string encodedHash)
    {
        try
        {
            var parts = encodedHash.Split('.', 4);
            if (parts.Length != 4 || parts[0] != "pbkdf2-sha256" ||
                !int.TryParse(parts[1], out var iterations) || iterations < 100_000)
            {
                return false;
            }

            var salt = Convert.FromBase64String(parts[2]);
            var expectedHash = Convert.FromBase64String(parts[3]);
            var actualHash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                expectedHash.Length);

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
