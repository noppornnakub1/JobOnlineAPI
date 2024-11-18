using System.Data;
using System.Dynamic;
using System.Text.Json;
using Dapper;
using JobOnlineAPI.Models;
using JobOnlineAPI.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace JobOnlineAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    public class ApplicantsController : ControllerBase
    {
        private readonly IApplicantRepository _applicantRepository;
        private readonly IJobApplicationRepository _jobApplicationRepository;

        public ApplicantsController(IApplicantRepository applicantRepository, IJobApplicationRepository jobApplicationRepository)
        {
            _applicantRepository = applicantRepository;
            _jobApplicationRepository = jobApplicationRepository;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Applicant>>> GetAllApplicants()
        {
            var applicants = await _applicantRepository.GetAllApplicantsAsync();
            return Ok(applicants);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Applicant>> GetApplicantById(int id)
        {
            var applicant = await _applicantRepository.GetApplicantByIdAsync(id);
            if (applicant == null)
            {
                return NotFound();
            }
            return Ok(applicant);
        }

        [HttpPost]
        public async Task<ActionResult<Applicant>> AddApplicant(Applicant applicant)
        {
            int newId = await _applicantRepository.AddApplicantAsync(applicant);
            applicant.ApplicantID = newId;
            return CreatedAtAction(nameof(GetApplicantById), new { id = newId }, applicant);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateApplicant(int id, Applicant applicant)
        {
            if (id != applicant.ApplicantID)
            {
                return BadRequest();
            }

            var existingApplicant = await _applicantRepository.GetApplicantByIdAsync(id);
            if (existingApplicant == null)
            {
                return NotFound();
            }

            await _applicantRepository.UpdateApplicantAsync(applicant);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteApplicant(int id)
        {
            var existingApplicant = await _applicantRepository.GetApplicantByIdAsync(id);
            if (existingApplicant == null)
            {
                return NotFound();
            }

            await _applicantRepository.DeleteApplicantAsync(id);
            return NoContent();
        }

        [HttpPost("upload-resume")]
        public async Task<IActionResult> UploadResume(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var filePath = Path.Combine("wwwroot/resumes", file.FileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return Ok(new { filePath });
        }

        [HttpPost("submit-application")]
        public async Task<IActionResult> SubmitApplication([FromForm] Applicant applicant, [FromForm] int jobId, [FromForm] IFormFile? resume)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (jobId == 0)
            {
                return BadRequest("JobID is required.");
            }

            if (resume != null && resume.Length > 0)
            {
                var resumesFolder = Path.Combine("wwwroot", "resumes");

                if (!Directory.Exists(resumesFolder))
                {
                    Directory.CreateDirectory(resumesFolder);
                }

                var filePath = Path.Combine(resumesFolder, resume.FileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await resume.CopyToAsync(stream);
                }

                applicant.Resume = filePath;
            }

            int applicantId = await _applicantRepository.AddApplicantAsync(applicant);

            var jobApplication = new JobApplication
            {
                ApplicantID = applicantId,
                JobID = jobId,
                Status = "Submitted",
                SubmissionDate = DateTime.Now
            };

            await _jobApplicationRepository.AddJobApplicationAsync(jobApplication);

            return Ok(new { applicantId, jobApplication });
        }

        [HttpPost("submit-application-dynamic")]
        public async Task<IActionResult> SubmitApplicationDynamic([FromBody] ExpandoObject request)
        {
            if (request == null || !((IDictionary<string, object?>)request).Any())
                return BadRequest("Invalid input.");

            var applicantParams = new DynamicParameters();
            foreach (var kvp in (IDictionary<string, object?>)request)
            {
                if (kvp.Value is JsonElement jsonElement)
                {
                    switch (jsonElement.ValueKind)
                    {
                        case JsonValueKind.String:
                            applicantParams.Add(kvp.Key, jsonElement.GetString());
                            break;
                        case JsonValueKind.Array:
                        case JsonValueKind.Object:
                            applicantParams.Add(kvp.Key, jsonElement.ToString());
                            break;
                        case JsonValueKind.Number:
                            if (jsonElement.TryGetInt32(out int intValue))
                                applicantParams.Add(kvp.Key, intValue);
                            else if (jsonElement.TryGetDouble(out double doubleValue))
                                applicantParams.Add(kvp.Key, doubleValue);
                            break;
                        case JsonValueKind.True:
                        case JsonValueKind.False:
                            applicantParams.Add(kvp.Key, jsonElement.GetBoolean());
                            break;
                        case JsonValueKind.Null:
                            applicantParams.Add(kvp.Key, null);
                            break;
                        default:
                            return BadRequest($"Unsupported JSON value for key '{kvp.Key}'.");
                    }
                }
                else
                {
                    applicantParams.Add(kvp.Key, kvp.Value);
                }
            }

            using var connection = _applicantRepository.GetConnection();
            var applicantId = await connection.QueryFirstOrDefaultAsync<int>(
                "InsertApplicantData",
                applicantParams,
                commandType: CommandType.StoredProcedure
            );
            return Ok(new { ApplicantID = applicantId });
        }
    }
}