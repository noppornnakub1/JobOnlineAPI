using Dapper;
using Microsoft.AspNetCore.Mvc;
using JobOnlineAPI.DAL;
using System.Text;
using System.Text.Json;
using JobOnlineAPI.Models;
using System.Data;

namespace JobOnlineAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoginAdminNewController(DapperContext context) : ControllerBase
    {
        private readonly DapperContext _context = context;
        
        [HttpPost]
        [ProducesResponseType(typeof(IEnumerable<dynamic>), StatusCodes.Status200OK)]
        public async Task<IActionResult> LoginAdmin([FromBody] LoginRequest request)
        {
            try
            {
                using var connection = _context.CreateConnection();
                var parameters = new DynamicParameters();
                if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password)) {
                    return BadRequest("Username and Password are required.");
                }

                parameters.Add("@Username", request.Username);

                var query = "EXEC sp_GetAdminUsersWithRole @Username";
                var result = await connection.QueryFirstOrDefaultAsync(query, parameters);

                if (result == null) return Unauthorized("User or password is Invalid.");
                string hashedPassword = result.Password;
                bool isPasswordMatch = BCrypt.Net.BCrypt.Verify(request.Password, hashedPassword);
                if(!isPasswordMatch) return Unauthorized("User or password is Invalid.");

                result.Password = "";
                
                return Ok(new {
                    AdminID = result.AdminID,
                    Username = result.Username,
                    Role = result.Role,
                    Department = result.Department
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}