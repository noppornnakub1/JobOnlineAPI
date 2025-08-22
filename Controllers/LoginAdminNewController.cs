using Dapper;
using Microsoft.AspNetCore.Mvc;
using JobOnlineAPI.DAL;

namespace JobOnlineAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoginAdminNewController(DapperContext context, IConfiguration configuration) : ControllerBase
    {
        private readonly DapperContext _context = context ?? throw new ArgumentNullException(nameof(context));
        private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        public class LoginRequestAdmin
        {
            public required string Username { get; set; }
        }

        [HttpPost("LoginAD")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> LoginAdminAD([FromBody] LoginRequestAdmin request)
        {
            if (string.IsNullOrWhiteSpace(request.Username))
                return BadRequest("Username is required and cannot be empty or whitespace.");

            try
            {
                using var connection = _context.CreateConnection();
                var parameters = new DynamicParameters();
                parameters.Add("@Username", request.Username);

                var user = await connection.QueryFirstOrDefaultAsync<dynamic>(
                    "EXEC sp_GetAdminUsersWithRoleV2 @Username", parameters);

                if (user == null)
                    return Unauthorized("Invalid username.");

                return Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = "Internal server error", Details = ex.Message });
            }
        }
    }
}