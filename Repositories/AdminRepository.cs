using System.Data;
using System.Threading.Tasks;
using Dapper;
using BCrypt.Net;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using JobOnlineAPI.Models;

namespace JobOnlineAPI.Repositories
{
    public class AdminRepository : IAdminRepository
    {
        private readonly string _connectionString;

        public AdminRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                                ?? throw new ArgumentNullException(nameof(_connectionString), "Connection string 'DefaultConnection' is not found.");
        }

        public async Task<int> AddAdminUserAsync(AdminUser admin)
        {
            using IDbConnection db = new SqlConnection(_connectionString);
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(admin.Password);
            admin.Password = hashedPassword;

            string sql = @"
                    INSERT INTO AdminUsers (Username, Password, Role)
                    VALUES (@Username, @Password, @Role);
                    SELECT CAST(SCOPE_IDENTITY() as int)";
            var id = await db.QuerySingleAsync<int>(sql, admin);
            return id;
        }

        public async Task<bool> VerifyPasswordAsync(string username, string password)
        {
            using IDbConnection db = new SqlConnection(_connectionString);
            string sql = "SELECT Password FROM AdminUsers WHERE Username = @Username";
            var storedHashedPassword = await db.QueryFirstOrDefaultAsync<string>(sql, new { Username = username });

            if (storedHashedPassword == null)
                return false;

            return BCrypt.Net.BCrypt.Verify(password, storedHashedPassword);
        }
    }
}