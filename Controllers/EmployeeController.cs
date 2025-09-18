using System.Data;
using Dapper;
using JobOnlineAPI.DAL;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;

namespace JobOnlineAPI.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class EmployeeController(IConfiguration configuration) : ControllerBase
    {
        private readonly IConfiguration _configuration = configuration;

        [HttpGet("CheckCodeId")]
        public async Task<IActionResult> CheckCodeId([FromQuery] string? comCode)
        {
            try
            {
                string? connectionString = _configuration.GetConnectionString("DefaultConnection");
                using var connection = new SqlConnection(connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@CodeMPID", comCode);
                parameters.Add("@Exists", dbType: System.Data.DbType.Boolean, direction: System.Data.ParameterDirection.Output);

                await connection.ExecuteAsync(
                    "sp_CheckCodeMPIDExists",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                bool exists = parameters.Get<bool>("@Exists");

                return Ok(new { exists });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

    }

}
