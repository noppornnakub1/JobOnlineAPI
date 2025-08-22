using Microsoft.AspNetCore.Mvc;
using JobOnlineAPI.Models;
using JobOnlineAPI.Repositories;
using JobOnlineAPI.Filters;
using System.Data;
using System.Data.SqlClient;
using Dapper;

namespace JobOnlineAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JobsController(IJobRepository jobRepository, IConfiguration configuration) : ControllerBase
    {
        private readonly IJobRepository _jobRepository = jobRepository ?? throw new ArgumentNullException(nameof(jobRepository));
        private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        /// <summary>
        /// ดึงรายการตำแหน่งงานทั้งหมด
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Job>>> GetAllJobs()
        {
            try
            {
                var jobs = await _jobRepository.GetAllJobsAsync();
                if (jobs == null || !jobs.Any())
                    return NotFound("No jobs found.");
                return Ok(jobs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = "Failed to retrieve jobs.", Details = ex.Message });
            }
        }

        /// <summary>
        /// ดึงตำแหน่งงานทั้งหมดโดยไม่มีการปิด (ไม่ใช้ filter)
        /// </summary>
        [HttpGet("all-without-closing")]
        public async Task<ActionResult<IEnumerable<Job>>> GetAllJobsWithoutClosingFilter([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (page <= 0 || pageSize <= 0)
                return BadRequest("Page and pageSize must be positive integers.");

            try
            {
                using var connection = new SqlConnection(_connectionString);
                var parameters = new { Page = page, PageSize = pageSize };
                var jobs = await connection.QueryAsync<Job>("sp_GetAllJobsWithoutClosingFilter", parameters, commandType: CommandType.StoredProcedure);
                if (!jobs.Any())
                    return NotFound("No jobs found without closing filter.");
                return Ok(jobs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = "Failed to retrieve jobs.", Details = ex.Message });
            }
        }

        /// <summary>
        /// ดึงข้อมูลตำแหน่งงานตาม ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<Job>> GetJobById(int id)
        {
            if (id <= 0)
                return BadRequest("Job ID must be a positive integer.");

            try
            {
                var job = await _jobRepository.GetJobByIdAsync(id);
                if (job == null)
                    return NotFound($"Job with ID {id} not found.");
                return Ok(job);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = "Failed to retrieve job.", Details = ex.Message });
            }
        }

        /// <summary>
        /// เพิ่มตำแหน่งงานใหม่
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<Job>> AddJob([FromBody] Job job)
        {
            if (job == null || string.IsNullOrWhiteSpace(job.JobTitle))
                return BadRequest("Job title is required.");

            try
            {
                int newId = await _jobRepository.AddJobAsync(job);
                job.JobID = newId;
                return CreatedAtAction(nameof(GetJobById), new { id = newId }, job);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = "Failed to add job.", Details = ex.Message });
            }
        }

        /// <summary>
        /// อัปเดตตำแหน่งงาน
        /// </summary>
        [HttpPut("{id}")]
        [TypeFilter(typeof(JwtAuthorizeAttribute))]
        public async Task<IActionResult> UpdateJob(int id, [FromBody] Job job)
        {
            if (id <= 0 || id != job.JobID)
                return BadRequest("Job ID is invalid or mismatched.");
            if (string.IsNullOrWhiteSpace(job.JobTitle))
                return BadRequest("Job title is required.");
            if (!User.IsInRole("Admin"))
                return Unauthorized("Only admins can update jobs.");

            try
            {
                var existingJob = await _jobRepository.GetJobByIdAsync(id);
                if (existingJob == null)
                    return NotFound($"Job with ID {id} not found.");

                int rowsAffected = await _jobRepository.UpdateJobAsync(job);
                if (rowsAffected <= 0)
                    return StatusCode(500, "Update failed.");
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = "Failed to update job.", Details = ex.Message });
            }
        }

        /// <summary>
        /// ลบตำแหน่งงาน
        /// </summary>
        [HttpDelete("{id}")]
        [TypeFilter(typeof(JwtAuthorizeAttribute))]
        public async Task<IActionResult> DeleteJob(int id)
        {
            if (id <= 0)
                return BadRequest("Job ID must be a positive integer.");
            if (!User.IsInRole("Admin"))
                return Unauthorized("Only admins can delete jobs.");

            try
            {
                var existingJob = await _jobRepository.GetJobByIdAsync(id);
                if (existingJob == null)
                    return NotFound($"Job with ID {id} not found.");

                await _jobRepository.DeleteJobAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = "Failed to delete job.", Details = ex.Message });
            }
        }
    }
}