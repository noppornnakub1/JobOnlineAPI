using Dapper;
using Microsoft.AspNetCore.Mvc;
using JobOnlineAPI.DAL;
using System.Data;

namespace JobOnlineAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DepartmentController(DapperContext context, DapperContextHRMS contextHRMS) : ControllerBase
    {
        private readonly DapperContext _context = context;
        private readonly DapperContextHRMS _contextHRMS = contextHRMS;

        [HttpGet("GetDepartment")]
        public async Task<IActionResult> GetDepartmentFromHRMS([FromQuery] string? comCode)
        {
            try
            {
                using var connection = _contextHRMS.CreateConnection();
                var parameters = new DynamicParameters();
                parameters.Add("@COMPANY_CODE", comCode);

                var result = await connection.QueryAsync(
                    "sp_GetDepartmentBycomCodeV2",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpGet("GetJobsDepartment")]
        public async Task<IActionResult> GetJobsDepartment()
        {
            try
            {
                using var connection = _context.CreateConnection();
                var result = await connection.QueryAsync(
                    "sp_GetJobsDepartment",
                    commandType: CommandType.StoredProcedure
                );

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }
    }
}