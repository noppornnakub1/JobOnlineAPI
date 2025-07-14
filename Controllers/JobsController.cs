using Microsoft.AspNetCore.Mvc;
using JobOnlineAPI.Models;
using JobOnlineAPI.Repositories;
using JobOnlineAPI.Filters;
using System.Data;
using System.Reflection;
using System.Data.SqlClient;

namespace JobOnlineAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JobsController(IJobRepository jobRepository, IConfiguration configuration) : ControllerBase
    {
        private readonly IJobRepository _jobRepository = jobRepository;
        private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Job>>> GetAllJobs()
        {
            var jobs = await _jobRepository.GetAllJobsAsync();
            return Ok(jobs);
        }

        [HttpGet("all-without-closing")]
        public async Task<ActionResult<IEnumerable<Job>>> GetAllJobsWithoutClosingFilter()
        {
            var jobs = new List<Job>();
            var connectionString = _configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException("Connection string 'DefaultConnection' is not found.");

            using (var connection = new SqlConnection(connectionString))
            {
                var command = new SqlCommand("sp_GetAllJobsWithoutClosingFilter", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();
                var schemaTable = reader.GetSchemaTable();
                var properties = typeof(Job).GetProperties();

                while (await reader.ReadAsync())
                {
                    var job = new Job();

                    foreach (DataRow schemaRow in schemaTable.Rows)
                    {
                        string columnName = schemaRow["ColumnName"]?.ToString() ?? string.Empty;
                        if (string.IsNullOrEmpty(columnName)) continue;

                        PropertyInfo? property = properties.FirstOrDefault(p => p.Name == columnName);

                        if (property != null && !reader.IsDBNull(reader.GetOrdinal(columnName)))
                        {
                            if (property.PropertyType == typeof(int) || property.PropertyType == typeof(int?))
                                property.SetValue(job, reader.GetInt32(columnName));
                            else if (property.PropertyType == typeof(string))
                                property.SetValue(job, reader.GetString(columnName));
                            else if (property.PropertyType == typeof(DateTime) || property.PropertyType == typeof(DateTime?))
                                property.SetValue(job, reader.GetDateTime(columnName));
                            else if (property.PropertyType == typeof(decimal) || property.PropertyType == typeof(decimal?))
                                property.SetValue(job, reader.GetDecimal(columnName));
                        }
                    }

                    jobs.Add(job);
                }
            }

            return Ok(jobs);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Job>> GetJobById(int id)
        {
            var job = await _jobRepository.GetJobByIdAsync(id);
            if (job == null)
            {
                return NotFound();
            }
            return Ok(job);
        }

        // [HttpGet("AllJobs")]
        // public async Task<ActionResult<Job>> GetAllobs([FromQuery] int? id)
        // {
        //     var job = await _jobRepository.GetJobsAsync(id);
        //     if (job == null)
        //     {
        //         return NotFound("ไม่พบข้อมูลตำแหน่งงาน");
        //     }
        //     return Ok(job);
        // }

        [HttpPost]
        public async Task<ActionResult<Job>> AddJob(Job job)
        {
            if (job == null)
            {
                return BadRequest();
            }

            int newId = await _jobRepository.AddJobAsync(job);
            job.JobID = newId;
            return CreatedAtAction(nameof(GetJobById), new { id = newId }, job);
        }

        [HttpPut("{id}")]
        [TypeFilter(typeof(JwtAuthorizeAttribute))]
        public async Task<IActionResult> UpdateJob(int id, Job job)
        {
            if (id != job.JobID)
            {
                return BadRequest("Job ID mismatch.");
            }

            var existingJob = await _jobRepository.GetJobByIdAsync(id);
            if (existingJob == null)
            {
                return NotFound("Job not found.");
            }

            int rowsAffected = await _jobRepository.UpdateJobAsync(job);
            if (rowsAffected <= 0)
            {
                return StatusCode(500, "Update failed.");
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        [TypeFilter(typeof(JwtAuthorizeAttribute))]
        public async Task<IActionResult> DeleteJob(int id)
        {
            var existingJob = await _jobRepository.GetJobByIdAsync(id);
            if (existingJob == null)
            {
                return NotFound();
            }

            await _jobRepository.DeleteJobAsync(id);
            return NoContent();
        }
    }
}