using Dapper;
using Microsoft.AspNetCore.Mvc;
using JobOnlineAPI.DAL;
using System.Text;
using System.Text.Json;
using JobOnlineAPI.Models;
using JobOnlineAPI.Services;
using System.Dynamic;
using System.Data;

namespace JobOnlineAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ApplicantNewController(DapperContext context, IEmailService emailService) : ControllerBase
    {
        private readonly DapperContext _context = context;
        private readonly IEmailService _emailService = emailService;

        [HttpGet("applicant")]
        [ProducesResponseType(typeof(IEnumerable<dynamic>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetApplicants()
        {
            try
            {
                using var connection = _context.CreateConnection();
                var query = "EXEC spGetAllApplicantsWithJobDetails";
                var applicants = await connection.QueryAsync(query);

                return Ok(applicants);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        [HttpGet("applicantByID")]
        [ProducesResponseType(typeof(IEnumerable<dynamic>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetApplicantsyById([FromQuery] int? ApplicantID)
        {
            try
            {
                using var connection = _context.CreateConnection();
                var parameters = new DynamicParameters();

                parameters.Add("@ApplicantID", ApplicantID);

                var query = "EXEC spGetAllApplicantsWithJobDetailsNew @ApplicantID";
                var applicants = await connection.QueryAsync(query, parameters);

                return Ok(applicants);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("addApplicant")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<IActionResult> PostApplicant([FromBody] Dictionary<string, object?> payload)
        {
            try
            {
                using var connection = _context.CreateConnection();
                var jsonPayload = JsonSerializer.Serialize(payload);

                var parameters = new DynamicParameters();
                parameters.Add("@JsonInput", jsonPayload);
                parameters.Add("@InsertedApplicantID", dbType: DbType.Int32, direction: ParameterDirection.Output);

                await connection.ExecuteAsync(
                    "sp_InsertApplicantNew",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                int insertedId = parameters.Get<int>("@InsertedApplicantID");

                return Ok(new
                {
                    Message = "Insert success",
                    ApplicantID = insertedId,
                    Data = payload
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("submit-application-dynamic")]
        public async Task<IActionResult> SubmitApplicationDynamic([FromBody] ExpandoObject request)
        {
            if (request is not IDictionary<string, object?> req || !req.TryGetValue("JobID", out var jobIdObj))
                return BadRequest("Invalid or missing JobID.");

            using var conn = _context.CreateConnection(); // ✅ แก้ตรงนี้
            var param = new DynamicParameters();

            // Extract list fields
            string[] listKeys = { "EducationList", "WorkExperienceList", "SkillsList" };
            foreach (var key in listKeys)
            {
                if (req.TryGetValue(key, out var val) && val is JsonElement je && je.ValueKind == JsonValueKind.Array)
                {
                    param.Add(key, je.GetRawText());
                    req.Remove(key); // remove from main JSON
                }
                else
                {
                    param.Add(key, "[]");
                }
            }
            int jobId = jobIdObj is JsonElement j && j.ValueKind == JsonValueKind.Number
                ? j.GetInt32()
                : Convert.ToInt32(jobIdObj); // fallback ถ้าเป็น int อยู่แล้ว

            param.Add("JobID", jobId);
            param.Add("JsonInput", JsonSerializer.Serialize(req));

            // Output
            param.Add("ApplicantID", dbType: DbType.Int32, direction: ParameterDirection.Output);
            param.Add("ApplicantEmail", dbType: DbType.String, direction: ParameterDirection.Output, size: 100);
            param.Add("HRManagerEmails", dbType: DbType.String, direction: ParameterDirection.Output, size: 500);
            param.Add("JobManagerEmails", dbType: DbType.String, direction: ParameterDirection.Output, size: 500);

            try
            {
                // await conn.ExecuteAsync("InsertApplicantDataV3", param, commandType: CommandType.StoredProcedure);
                await conn.ExecuteAsync("InsertApplicantDataV4", param, commandType: CommandType.StoredProcedure);

                var id = param.Get<int>("ApplicantID");
                var email = param.Get<string>("ApplicantEmail");
                var hrEmails = param.Get<string>("HRManagerEmails");
                var jobEmails = param.Get<string>("JobManagerEmails");

                // if (!string.IsNullOrWhiteSpace(email))
                // {
                //     await _emailService.SendEmailAsync(email, "Application Received",
                //         $"<p>Your application (ID: {id}) has been submitted.</p>", true);
                // }

                // foreach (var e in $"{hrEmails},{jobEmails}".Split(',').Distinct())
                // {
                //     if (!string.IsNullOrWhiteSpace(e))
                //         await _emailService.SendEmailAsync(e.Trim(), "New Job Application",
                //             $"<p>New application submitted for JobID: {jobIdObj}.</p>", true);
                // }

                return Ok(new { ApplicantID = id, Message = "Submitted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = "Server error", ex.Message });
            }
        }

        [HttpGet("GetCandidate")]
        public async Task<IActionResult> GetFilteredCandidates([FromQuery] string? department, [FromQuery] int? jobId)
        {
            try
            {
                using var connection = _context.CreateConnection();

                var parameters = new DynamicParameters();
                parameters.Add("@Department", department);
                parameters.Add("@JobID", jobId);

                var result = await connection.QueryAsync(
                    "sp_GetCandidateAll",
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

        [HttpGet("GetCandidateData")]
        public async Task<IActionResult> GetApplicantData([FromQuery] int? id)
        {
            try
            {
                using var connection = _context.CreateConnection();

                var parameters = new DynamicParameters();
                parameters.Add("@ApplicantID", id);

                var result = await connection.QueryAsync(
                    "sp_GetApplicantDataV2",
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



        [HttpPut("updateApplicantStatus")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateApplicantStatus([FromBody] ExpandoObject request)
        {
            try
            {
                using var connection = _context.CreateConnection();
                var parameters = new DynamicParameters();

                var data = request as IDictionary<string, object>;
                if (!data.ContainsKey("ApplicantID") || !data.ContainsKey("Status"))
                    return BadRequest("Missing required fields: ApplicantID or Status");

                var applicantId = ((JsonElement)data["ApplicantID"]).GetInt32();
                var status = ((JsonElement)data["Status"]).GetString();

                parameters.Add("@ApplicantID", applicantId);
                parameters.Add("@Status", status);

                var query = "EXEC sp_UpdateApplicantStatus @ApplicantID, @Status";
                await connection.ExecuteAsync(query, parameters);

                return Ok(new { message = "อัปเดตสถานะเรียบร้อย" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPut("updateJobApprovalStatus")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateJobApprovalStatus([FromBody] ExpandoObject request)
        {
            try
            {
                using var connection = _context.CreateConnection();
                var parameters = new DynamicParameters();

                var data = request as IDictionary<string, object>;
                if (!data.ContainsKey("JobID") || !data.ContainsKey("ApprovalStatus"))
                    return BadRequest("Missing required fields: JobID or ApprovalStatus");

                var jobId = ((JsonElement)data["JobID"]).GetInt32();
                var approvalStatus = ((JsonElement)data["ApprovalStatus"]).GetString();

                parameters.Add("@JobID", jobId);
                parameters.Add("@ApprovalStatus", approvalStatus);

                var query = "EXEC sp_UpdateJobApprovalStatus @JobID, @ApprovalStatus";
                await connection.ExecuteAsync(query, parameters);

                return Ok(new { message = "อัปเดตสถานะของงานเรียบร้อย" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


    }
}
