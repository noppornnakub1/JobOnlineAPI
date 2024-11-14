using System.Data;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using JobOnlineAPI.Models;

namespace JobOnlineAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RegisterController : ControllerBase
    {
        private readonly IDbConnection _dbConnection;

        public RegisterController(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        [HttpPost]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var passwordHash = HashPassword(model.Password);

            var result = await _dbConnection.ExecuteScalarAsync<int>(
                "RegisterUser",
                new
                {
                    model.Email,
                    PasswordHash = passwordHash,
                    CreatedAt = DateTime.Now
                },
                commandType: CommandType.StoredProcedure
            );

            if (result == -1)
            {
                return BadRequest("Email already exists.");
            }
            else if (result == 1)
            {
                return Ok("User registered successfully.");
            }
            else
            {
                return StatusCode(500, "An error occurred while registering the user.");
            }
        }

        private static string HashPassword(string password)
        {
            var hashedBytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
        }
    }
}