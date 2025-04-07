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

                Console.WriteLine($"Applicant id: {id}");
                Console.WriteLine($"Applicant Email: {email}");
                Console.WriteLine($"HR Manager Emails: {hrEmails}");
                Console.WriteLine($"Job Manager Emails: {jobEmails}");

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

                var candidateJson = data.ContainsKey("Candidate") ? data["Candidate"].ToString() : null;
                    dynamic? candidate = !string.IsNullOrEmpty(candidateJson) 
                        ? JsonSerializer.Deserialize<ExpandoObject>(candidateJson) 
                        : null;
                var EmailSend = data.ContainsKey("EmailSend") ? ((JsonElement)data["EmailSend"]).GetString() : null;


                parameters.Add("@ApplicantID", applicantId);
                parameters.Add("@Status", status);

                var query = "EXEC sp_UpdateApplicantStatus @ApplicantID, @Status";
                await connection.ExecuteAsync(query, parameters);



                string hrBody = $@"
                    <div style='font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px;'>
                        <table style='width: 100%; max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 10px rgba(0,0,0,0.1);'>
                            <tr>
                                <td style='background-color: #2E86C1; padding: 20px; text-align: center; color: #ffffff;'>
                                    <h2 style='margin: 0; font-size: 24px;'>Application Status Updated</h2>
                                </td>
                            </tr>
                            <tr>
                                <td style='padding: 20px; color: #333;'>
                                    <p style='font-size: 16px;'>มีการอัปเดตสถานะของผู้สมัครงานในตำแหน่ง <strong>{candidate?.jobTitle}</strong>.</p>
                                    <p style='font-size: 14px;'><strong>ข้อมูลผู้สมัคร:</strong></p>
                                    <ul style='font-size: 14px; line-height: 1.6;'>
                                        <li><strong>ชื่อ:</strong> {candidate?.firstNameThai} {candidate?.lastNameThai}</li>
                                        <li><strong>ชื่ออังกฤษ:</strong> {candidate?.firstNameEng} {candidate?.lastNameEng}</li>
                                        <li><strong>อีเมล:</strong> {candidate?.email}</li>
                                        <li><strong>เบอร์โทร:</strong> {candidate?.mobilePhone}</li>
                                        <li><strong>สถานะใหม่:</strong> {status}</li>
                                        <li><strong>ผู้ดำเนินการ:</strong> {EmailSend}</li>
                                    </ul>
                                </td>
                            </tr>
                            <tr>
                                <td style='background-color: #2E86C1; padding: 10px; text-align: center; color: #ffffff;'>
                                    <p style='margin: 0; font-size: 12px;'>This is an automated message. Please do not reply to this email.</p>
                                </td>
                            </tr>
                        </table>
                    </div>";
                // <p style='font-size: 14px;'>กรุณาตรวจสอบข้อมูลเพิ่มเติมที่ระบบ HR.</p>
                // <p style='font-size: 14px;'>
                //     <a href='https://yourdomain.com/ApplicationForm/ApplicationFormView?id={candidate?.applicantID}' target='_blank' style='color: #2E86C1; text-decoration: underline;'>คลิกที่นี่เพื่อดูข้อมูลผู้สมัคร</a>
                // </p>
                var queryStaff = "EXEC GetStaffByEmail @Role = @Role";
                var staffList = await connection.QueryAsync<dynamic>(queryStaff, new { Role = "HR Manager" });
                int successCount = 0;
                int failCount = 0;
                foreach (var staff in staffList)
                {
                    var hrEmail = staff.Email;
                    if (!string.IsNullOrWhiteSpace(hrEmail))
                    {
                        try
                        {
                            await _emailService.SendEmailAsync(hrEmail, "New Job Application", hrBody, true);
                            successCount++;
                        }
                        catch (Exception ex)
                        {
                            failCount++;
                            Console.WriteLine($"❌ Failed to send email to {hrEmail}: {ex.Message}");
                        }
                    }
                }
                
                return Ok(new { message = "อัปเดตสถานะเรียบร้อย", sendMail = successCount });
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
