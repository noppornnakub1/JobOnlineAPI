using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;
using Dapper;

namespace JobOnlineAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TServiceController(IConfiguration configuration) : ControllerBase
    {
        private readonly IConfiguration _configuration = configuration;

        [HttpGet("getTService")]
        public async Task<IActionResult> GetYearRange()
        {
            string? connectionString = _configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                return StatusCode(500, new { message = "Connection string is missing or not configured." });
            }

            try
            {
                using var connection = new SqlConnection(connectionString);
                var resultTService = await connection.QueryAsync<dynamic>(
                    "sp_GetAllTService",
                    commandType: CommandType.StoredProcedure);

                return Ok(resultTService);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while retrieving year range.",
                    error = ex.Message
                });
            }
        }
    }
}