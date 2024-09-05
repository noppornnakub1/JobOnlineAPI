using System.Collections.Generic;
using System.Threading.Tasks;
using JobOnlineAPI.Models;
using JobOnlineAPI.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace JobOnlineAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class JobsController : ControllerBase
    {
        private readonly IJobRepository _jobRepository;

        public JobsController(IJobRepository jobRepository)
        {
            _jobRepository = jobRepository;
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
    }
}