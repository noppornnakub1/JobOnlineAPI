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

            using var conn = _context.CreateConnection();
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

                req.TryGetValue("jobDepartment", out var jobDepartmentObj);
                var jobDepartment = JsonSerializer.Deserialize<string>(jobDepartmentObj?.ToString() ?? "");

                var emailParameters = new DynamicParameters();
                emailParameters.Add("@Role", null);
                emailParameters.Add("@Department", jobDepartment);
                emailParameters.Add("@Type", "Register");

                var applicantEmail = param.Get<string>("ApplicantEmail");
                var hrManagerEmails = param.Get<string>("HRManagerEmails");
                var jobManagerEmails = param.Get<string>("JobManagerEmails");
                var JobTitle = param.Get<string>("JobTitle");
                var FullNameEng = $"{param.Get<string>("FirstNameEng")} {param.Get<string>("LastNameEng")}";
                var FullNameThai = $"{param.Get<string>("FirstNameThai")} {param.Get<string>("LastNameThai")}";
                var CompanyName = param.Get<string>("comName");
                var DeptName = param.Get<string>("DeptName") ?? "-";
                var Mobile = param.Get<string>("Mobile") ?? "-";
                var POST = param.Get<string>("POST") ?? "-";
                var results = await conn.QueryAsync<dynamic>(
                    "sp_GetDateSendEmailV2",
                    new { Role = 2, Department = "10807", Type = "" },
                    commandType: CommandType.StoredProcedure
                );
                var first = results.FirstOrDefault();
                string Tel = first?.TELOFF ?? "-";
                string resultMail = first?.EMAIL ?? "-";
                if (!string.IsNullOrEmpty(applicantEmail))
                {
                    string applicantBody = $@"
                    <div style='font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px; font-size: 14px; line-height: 1.6;'>
                        <p style='margin: 0; font-weight: bold;'>{CompanyName}: ได้รับใบสมัครงานของคุณแล้ว</p>
                        <p style='margin: 0;'>เรียน คุณ {FullNameThai}</p>

                        <p>
                            ขอบคุณสำหรับความสนใจในตำแหน่ง <strong>{JobTitle}</strong> ที่บริษัท <strong>{CompanyName}</strong> ของเรา<br>
                            เราขอยืนยันว่าได้รับใบสมัครของท่านเรียบร้อยแล้ว ทีมงานฝ่ายทรัพยากรบุคคลของเรากำลังพิจารณาใบสมัครของท่าน และจะติดต่อกลับภายใน 7-14 วันทำการ หากคุณสมบัติของท่านตรงตามที่เรากำลังมองหา<br><br>
                            หากท่านมีข้อสงสัยหรือต้องการข้อมูลเพิ่มเติม สามารถติดต่อเราได้ที่อีเมล 
                            <span style='color: blue;'>{resultMail}</span> หรือโทร 
                            <span style='color: blue;'>{Tel}</span><br>
                            ขอบคุณอีกครั้งสำหรับความสนใจร่วมงานกับเรา
                        </p>

                        <p style='margin-top: 30px;'>ด้วยความเคารพ,</p>
                        <p style='margin: 0;'>{FullNameThai}</p>
                        <p style='margin: 0;'>ฝ่ายทรัพยากรบุคคล</p>
                        <p style='margin: 0;'>{CompanyName}</p>
                        <br>

                        <p style='color:red; font-weight: bold;'>**อีเมลนี้คือข้อความอัตโนมัติ กรุณาอย่าตอบกลับ**</p>
                    </div>";
                   await _emailService.SendEmailAsync(applicantEmail, "Application Received", applicantBody, true);
                }

                var managerEmails = $"{hrManagerEmails},{jobManagerEmails}".Split(',');
                foreach (var emailStaff in managerEmails.Distinct())
                {
                    if (!string.IsNullOrWhiteSpace(emailStaff))
                    {
                        string managerBody = $@"
                        <div style='font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px; font-size: 14px; line-height: 1.6;'>
                            <p style='margin: 0;'>เรียน คุณสมศรี (ผู้จัดการฝ่ายบุคคล) และ คุณคนขอเปิดตำแหน่ง ({POST})</p>
                            <p style='margin: 0;'>เรื่อง: แจ้งข้อมูลผู้สมัครตำแหน่ง <strong>{JobTitle}</strong></p>

                            <br>

                            <p style='margin: 0;'>เรียนทุกท่าน</p>
                            <p style='margin: 0;'>ทางฝ่ายรับสมัครงานขอแจ้งให้ทราบว่า คุณ <strong>{FullNameThai}</strong> ได้ทำการสมัครงานเข้ามาในตำแหน่ง <strong>{JobTitle}</strong></p>

                            <p style='margin: 0;'>กรุณาคลิก Link:
                                <a target='_blank' href='https://oneejobs.oneeclick.co:7191/ApplicationForm/ApplicationFormView?id={id}' 
                                style='color: #007bff; text-decoration: underline;'>
                                https://oneejobs.oneeclick.co
                                </a>
                                เพื่อดูรายละเอียดและดำเนินการในขั้นตอนต่อไป
                            </p>

                            <br>

                            <p style='color: red; font-weight: bold;'>**อีเมลนี้คือข้อความอัตโนมัติ กรุณาอย่าตอบกลับ**</p>
                        </div>";
                        await _emailService.SendEmailAsync(email.Trim(), "New Job Application Received", managerBody, true);
                    }
                }

                return Ok(new { ApplicantID = id, Message = "Application submitted and emails sent successfully." });
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

                var requesterMail = ((JsonElement)data["Email"]).GetString() ?? "-";
                var requesterName = ((JsonElement)data["NAMETHAI"]).GetString() ?? "-";
                var Tel = ((JsonElement)data["Mobile"]).GetString() ?? "-";
                var requesterPost = ((JsonElement)data["POST"]).GetString() ?? "-";
                var TelOff = ((JsonElement)data["TELOFF"]).GetString() ?? "-";
                var JobTitle = ((JsonElement)data["Department"]).GetString() ?? "-";

                parameters.Add("@ApplicantID", applicantId);
                parameters.Add("@Status", status);

                var query = "EXEC sp_UpdateApplicantStatus @ApplicantID, @Status";
                await connection.ExecuteAsync(query, parameters);


                // Application Status Updated
                string hrBody = $@"
                  <div style='font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px; font-size: 14px;'>
                    <p style='font-weight: bold; margin: 0 0 10px 0;'>เรียน คุณสมศรี (ผู้จัดการฝ่ายบุคคล)</p>
                    <p style='font-weight: bold; margin: 0 0 10px 0;'>เรื่อง: การเรียกสัมภาษณ์ผู้สมัครตำแหน่ง {JobTitle}</p>
                    <br>
                    <p style='margin: 0 0 10px 0;'>
                        เรียน ฝ่ายบุคคล<br>
                        ตามที่ได้รับแจ้งข้อมูลผู้สมัครในตำแหน่ง {JobTitle} จำนวน 3 ท่าน ผมได้พิจารณาประวัติและคุณสมบัติเบื้องต้นแล้ว และประสงค์จะขอเรียกผู้สมัครดังต่อไปนี้เข้ามาสัมภาษณ์
                    </p>
                    <p style='font-weight: bold; margin: 0 0 10px 0;'>คุณวิภาดา สุขสวัสดิ์</p>
                    <p style='margin: 0 0 10px 0;'>
                        จากข้อมูลผู้สมัคร ดิฉัน/ผมเห็นว่าคุณวิภาดามีประสบการณ์ด้านการตลาดดิจิทัลที่ตรงกับความต้องการของตำแหน่งงาน และมีความเชี่ยวชาญในทักษะที่จำเป็นต่อการทำงานในทีมของเรา
                    </p>
                    <p style='font-weight: bold; margin: 0 0 10px 0;'>ช่วงเวลาที่สะดวกในการสัมภาษณ์</p>
                    <p style='font-weight: bold; margin: 0 0 10px 0;'>วันอังคารที่ 22 เมษายน 2568 เวลา 10.00-11.30 น.</p>
                    <br>
                    <p style='margin: 0 0 10px 0;'>ขอความกรุณาฝ่ายบุคคลประสานงานกับผู้สมัครเพื่อนัดหมายการสัมภาษณ์ตามช่วงเวลาที่แจ้งไว้</p>
                    <p style='margin: 0 0 10px 0;'>หากท่านมีข้อสงสัยประการใด กรุณาติดต่อได้ที่เบอร์ด้านล่าง</p>
                    <p style='margin: 0 0 10px 0;'>ขอบคุณสำหรับความช่วยเหลือ</p>
                    <p style='margin: 0 0 10px 0;'>ขอแสดงความนับถือ</p>
                    <p style='margin: 0 0 10px 0;'>{requesterName}</p>
                    <p style='margin: 0 0 10px 0;'>{requesterPost}</p>
                    <p style='margin: 0 0 10px 0;'>โทร: {Tel} ต่อ {TelOff}</p>
                    <p style='margin: 0 0 10px 0;'>อีเมล: {requesterMail}</p>
                    <br>
                    <p style='color: red; font-weight: bold;'>**อีเมลนี้เป็นข้อความอัตโนมัติ กรุณาอย่าตอบกลับ**</p>
                </div>";

                var emailParameters = new DynamicParameters();
                emailParameters .Add("@Role", 2);
                emailParameters .Add("@Department", null);


                var queryStaff = "EXEC sp_GetDateSendEmail @Role = @Role, @Department = @Department";
                var staffList = await connection.QueryAsync<dynamic>(queryStaff, emailParameters);
                int successCount = 0;
                int failCount = 0;
                foreach (var staff in staffList)
                {
                    var hrEmail = staff.EMAIL;
                    if (!string.IsNullOrWhiteSpace(hrEmail))
                    {
                        try
                        {
                            await _emailService.SendEmailAsync(hrEmail, "Selected cantidate list", hrBody, true);
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

        [HttpGet("GetPDPAContent")]
        public async Task<IActionResult> GetPDPAContent()
        {
            try
            {
                using var connection = _context.CreateConnection();

                var result = await connection.QueryFirstOrDefaultAsync(
                    "sp_GetDataPDPA",
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
