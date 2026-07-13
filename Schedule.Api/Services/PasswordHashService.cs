using System.Security.Cryptography;

namespace Schedule.Api.Services;

public class PasswordHashService
{
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
