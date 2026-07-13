using System.Data;
using Dapper;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using Schedule.Core.Interfaces;
using Schedule.Core.Models;

namespace Schedule.Infrastructure.Repositories;

public class ApplicationUserRepository : IApplicationUserRepository
{
    private readonly string _connectionString;

    public ApplicationUserRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Рядок підключення не знайдено.");
    }

    public async Task<ApplicationUser?> GetByUserNameAsync(string userName)
    {
        const string sql = """
            SELECT
                `Id`,
                `UserName`,
                `DisplayName`,
                `PasswordHash`,
                `Role`,
                `IsActive`
            FROM `ApplicationUsers`
            WHERE `UserName` = @UserName
            LIMIT 1;
            """;

        using IDbConnection connection = new MySqlConnection(_connectionString);
        return await connection.QuerySingleOrDefaultAsync<ApplicationUser>(sql, new { UserName = userName });
    }
}
