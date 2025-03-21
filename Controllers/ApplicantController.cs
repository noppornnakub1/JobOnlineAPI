using System.Data;
using System.Dynamic;
using System.Text.Json;
using Dapper;
using JobOnlineAPI.Models;
using JobOnlineAPI.Repositories;
using JobOnlineAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace JobOnlineAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    public class ApplicantsController(IApplicantRepository applicantRepository, IJobApplicationRepository jobApplicationRepository, IEmailService emailService) : ControllerBase
    {
        private readonly IApplicantRepository _applicantRepository = applicantRepository;
        private readonly IJobApplicationRepository _jobApplicationRepository = jobApplicationRepository;
        private readonly IEmailService _emailService = emailService;

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

                 //applicant.Resume = filePath;
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

            var requestDictionary = (IDictionary<string, object?>)request;

            if (!requestDictionary.TryGetValue("JobID", out var jobIdObj) || jobIdObj == null)
                return BadRequest("JobID is required.");

            using var connection = _applicantRepository.GetConnection();

            var parameters = new DynamicParameters();

            foreach (var kvp in requestDictionary)
            {
                if (kvp.Value is JsonElement jsonElement)
                {
                    switch (jsonElement.ValueKind)
                    {
                        case JsonValueKind.String:
                            var stringValue = jsonElement.GetString();
                            parameters.Add(
                                kvp.Key,
                                stringValue,
                                dbType: DbType.String,
                                size: stringValue?.Length > 0 ? stringValue.Length : 1
                            );
                            break;
                        case JsonValueKind.Number:
                            if (jsonElement.TryGetInt32(out int intValue))
                                parameters.Add(kvp.Key, intValue, dbType: DbType.Int32);
                            else if (jsonElement.TryGetDouble(out double doubleValue))
                                parameters.Add(kvp.Key, doubleValue, dbType: DbType.Double);
                            break;
                        case JsonValueKind.True:
                        case JsonValueKind.False:
                            parameters.Add(kvp.Key, jsonElement.GetBoolean(), dbType: DbType.Boolean);
                            break;

                        case JsonValueKind.Array:
                        case JsonValueKind.Object:
                            var rawJson = jsonElement.GetRawText();
                            parameters.Add(kvp.Key, rawJson, DbType.String, size: rawJson.Length);
                            break;
                        case JsonValueKind.Null:
                            parameters.Add(kvp.Key, null);
                            break;
                        default:
                            return BadRequest($"Unsupported JSON value for key '{kvp.Key}'.");
                    }
                }
                else
                {
                    if (kvp.Value is string strValue)
                    {
                        parameters.Add(
                            kvp.Key,
                            strValue,
                            dbType: DbType.String,
                            size: strValue.Length > 0 ? strValue.Length : 1
                        );
                    }
                    else
                    {
                        parameters.Add(kvp.Key, kvp.Value);
                    }
                }
            }

            parameters.Add("ApplicantID", dbType: DbType.Int32, direction: ParameterDirection.Output);
            parameters.Add("ApplicantEmail", dbType: DbType.String, direction: ParameterDirection.Output, size: 100);
            parameters.Add("HRManagerEmails", dbType: DbType.String, direction: ParameterDirection.Output, size: 500);
            parameters.Add("JobManagerEmails", dbType: DbType.String, direction: ParameterDirection.Output, size: 500);

            try
            {
                await connection.ExecuteAsync(
                    "InsertApplicantDataV2",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var applicantId = parameters.Get<int?>("ApplicantID");
                if (applicantId == null)
                {
                    return BadRequest("ApplicantID was not generated by the stored procedure.");
                }

                var applicantEmail = parameters.Get<string>("ApplicantEmail");
                var hrManagerEmails = parameters.Get<string>("HRManagerEmails");
                var jobManagerEmails = parameters.Get<string>("JobManagerEmails");

                if (!string.IsNullOrEmpty(applicantEmail))
                {
                    var applicantBody = $"<p>Dear Applicant, Your application (ID: {applicantId}) has been submitted successfully.</p>";
                    await _emailService.SendEmailAsync(applicantEmail, "Application Received", applicantBody, true);
                }

                var managerEmails = $"{hrManagerEmails},{jobManagerEmails}".Split(',');
                foreach (var email in managerEmails.Distinct())
                {
                    if (!string.IsNullOrWhiteSpace(email))
                    {
                        var managerBody = $"<p>A new application has been submitted for JobID: {jobIdObj}.</p>";
                        await _emailService.SendEmailAsync(email.Trim(), "New Job Application", managerBody, true);
                    }
                }

                return Ok(new { ApplicantID = applicantId, Message = "Application submitted and emails sent successfully." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred: {ex.Message}");
                return StatusCode(500, new { Error = "Internal Server Error", Details = ex.Message });
            }
        }
    }
}