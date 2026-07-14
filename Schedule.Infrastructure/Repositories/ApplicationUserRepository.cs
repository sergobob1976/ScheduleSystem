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

    public async Task<IEnumerable<ApplicationUser>> GetAllAsync()
    {
        const string sql = """
            SELECT
                `Id`, `UserName`, `DisplayName`, `PasswordHash`, `Role`, `IsActive`
            FROM `ApplicationUsers`
            ORDER BY
                CASE WHEN `Role` = 'Administrator' THEN 0 ELSE 1 END,
                `DisplayName`;
            """;

        using IDbConnection connection = new MySqlConnection(_connectionString);
        return await connection.QueryAsync<ApplicationUser>(sql);
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

    public async Task<bool> CreateAsync(ApplicationUser user)
    {
        const string sql = """
            INSERT INTO `ApplicationUsers`
                (`UserName`, `DisplayName`, `PasswordHash`, `Role`, `IsActive`)
            VALUES
                (@UserName, @DisplayName, @PasswordHash, @Role, @IsActive);
            """;

        using IDbConnection connection = new MySqlConnection(_connectionString);
        return await connection.ExecuteAsync(sql, user) > 0;
    }

    public async Task<bool> UpdatePasswordHashAsync(int id, string passwordHash)
    {
        const string sql = """
            UPDATE `ApplicationUsers`
            SET `PasswordHash` = @PasswordHash
            WHERE `Id` = @Id;
            """;

        using IDbConnection connection = new MySqlConnection(_connectionString);
        return await connection.ExecuteAsync(sql, new { Id = id, PasswordHash = passwordHash }) > 0;
    }

    public async Task<bool> DeleteDispatcherAsync(int id)
    {
        const string sql = """
            DELETE FROM `ApplicationUsers`
            WHERE `Id` = @Id
              AND `Role` = 'Dispatcher';
            """;

        using IDbConnection connection = new MySqlConnection(_connectionString);
        return await connection.ExecuteAsync(sql, new { Id = id }) > 0;
    }
}
