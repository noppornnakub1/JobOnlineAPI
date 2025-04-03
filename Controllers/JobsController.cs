using System.Collections.Generic;
using System.Threading.Tasks;
using JobOnlineAPI.Models;
using JobOnlineAPI.Repositories;
using Microsoft.AspNetCore.Mvc;
using Dapper;
using System.Data;
using JobOnlineAPI.DAL;
namespace JobOnlineAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class JobsController : ControllerBase
    {
        private readonly IJobRepository _jobRepository;
        private readonly DapperContext _context;
        private readonly DapperContextHRMS _contextHRMS;

        public JobsController(IJobRepository jobRepository, DapperContext context, DapperContextHRMS contextHRMS)
        {
            _jobRepository = jobRepository;
            _context = context;
            _contextHRMS = contextHRMS;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Job>>> GetAllJobs()
        {
            var jobs = await _jobRepository.GetAllJobsAsync();
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
        public async Task<IActionResult> UpdateJob(int id, Job job)
        {
            if (id != job.JobID)
            {
                return BadRequest();
            }

            var existingJob = await _jobRepository.GetJobByIdAsync(id);
            if (existingJob == null)
            {
                return NotFound();
            }

            await _jobRepository.UpdateJobAsync(job);
            return NoContent();
        }

        [HttpDelete("{id}")]
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


        [HttpDelete("deleteJob/{id}")]
        public async Task<IActionResult> DeleteJobByJobID(int id)
        {
            try
            {
                using var connection = _context.CreateConnection();
                var parameters = new DynamicParameters();
                parameters.Add("@JobID", id);

                var remainingJobs = await connection.QueryAsync<Job>(
                    "sp_DeleteJobByJobID",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return Ok(new
                {
                    Message = "Job deleted successfully.",
                    RemainingJobs = remainingJobs
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = "Failed to delete job", ex.Message });
            }
        }

        [HttpGet("GetDepartment")]
        public async Task<IActionResult> GetDepartmentFromHRMS([FromQuery] string? comCode)
        {
            try
            {
                using var connection = _contextHRMS.CreateConnection();

                var parameters = new DynamicParameters();
                parameters.Add("@COMPANY_CODE", comCode);

                var result = await connection.QueryAsync(
                    "sp_GetDepartmentBycomCode",
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
          

    }
}