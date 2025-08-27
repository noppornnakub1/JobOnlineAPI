using Dapper;
using Microsoft.AspNetCore.Mvc;
using JobOnlineAPI.DAL;
using System.Text.Json;
using JobOnlineAPI.Services;
using System.Dynamic;
using System.Data;
using JobOnlineAPI.Filters;
using JobOnlineAPI.Models;
using Microsoft.Extensions.Options;

namespace JobOnlineAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ApplicantNewController : ControllerBase
    {
        private readonly DapperContext _context;
        private readonly IEmailService _emailService;
        private readonly FileProcessingService _fileProcessingService;
        private readonly INetworkShareService _networkShareService;
        private readonly ILogger<ApplicantNewController> _logger;
        private readonly IEmailNotificationService _emailNotificationService;
        private readonly string _applicationFormUri;
        private const string JobTitleKey = "JobTitle";
        private const string JobIdKey = "JobID";
        private const string ApplicantIdKey = "ApplicantID";
        private const string UserIdKey = "UserId";

        private sealed record JobApprovalData(
            int JobId,
            string ApprovalStatus,
            string? Remark);

        public ApplicantNewController(
            DapperContext context,
            IEmailService emailService,
            FileProcessingService fileProcessingService,
            INetworkShareService networkShareService,
            ILogger<ApplicantNewController> logger,
            IEmailNotificationService emailNotificationService,
            IOptions<FileStorageConfig> config)
        {
            _context = context;
            _emailService = emailService;
            _fileProcessingService = fileProcessingService;
            _networkShareService = networkShareService;
            _logger = logger;
            _emailNotificationService = emailNotificationService;
            var fileStorageConfig = config.Value ?? throw new ArgumentNullException(nameof(config));
            _applicationFormUri = fileStorageConfig.ApplicationFormUri ?? throw new InvalidOperationException("Application form URI is not configured.");
        }

        [HttpPost("submit-application-with-filesV2")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> SubmitApplicationWithFilesV2([FromForm] IFormFileCollection files, [FromForm] string jsonData)
        {
            try
            {
                if (string.IsNullOrEmpty(jsonData))
                    return BadRequest("JSON data is required.");

                var request = JsonSerializer.Deserialize<ExpandoObject>(jsonData);
                if (request is not IDictionary<string, object?> req || !req.TryGetValue(JobIdKey, out var jobIdObj) || jobIdObj == null)
                    return BadRequest("Invalid or missing JobID.");

                int jobId = jobIdObj is JsonElement j && j.ValueKind == JsonValueKind.Number
                    ? j.GetInt32()
                    : Convert.ToInt32(jobIdObj);

                await _networkShareService.ConnectAsync();
                try
                {
                    
                    var fileMetadatas = await _fileProcessingService.ProcessFilesAsync(files);
                    var dbResult = await SaveApplicationToDatabaseAsync(req, jobId, fileMetadatas);
                    _fileProcessingService.MoveFilesToApplicantDirectory(dbResult.ApplicantId, fileMetadatas);
                    await _emailNotificationService.SendApplicationEmailsAsync(req, dbResult, _applicationFormUri);
                    return Ok(new
                    {
                        ApplicantID = dbResult.ApplicantId,
                        FileMetadatas = fileMetadatas,
                        StorageLocation = _networkShareService.GetBasePath(),
                        Message = "Application and files submitted successfully."
                    });
                }
                finally
                {
                    _networkShareService.Disconnect();
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize JSON data: {Message}", ex.Message);
                return BadRequest("Invalid JSON data.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing application: {Message}", ex.Message);
                return StatusCode(500, new { Error = "Server error", ex.Message });
            }
        }

        private async Task<(int ApplicantId, string ApplicantEmail, string HrManagerEmails, string JobManagerEmails, string JobTitle, string CompanyName, int OutJobID)> SaveApplicationToDatabaseAsync(IDictionary<string, object?> req, int jobId, List<Dictionary<string, object>> fileMetadatas)
        {
            using var conn = _context.CreateConnection();
            var param = new DynamicParameters();

            string[] listKeys = ["EducationList", "WorkExperienceList", "SkillsList"];
            foreach (var key in listKeys)
            {
                param.Add(key, req.TryGetValue(key, out var val) && val is JsonElement je && je.ValueKind == JsonValueKind.Array
                    ? je.GetRawText()
                    : "[]");
            }

            param.Add("JsonInput", JsonSerializer.Serialize(req));
            param.Add("FilesList", JsonSerializer.Serialize(fileMetadatas));
            param.Add("JobID", jobId);
            param.Add("ApplicantID", dbType: DbType.Int32, direction: ParameterDirection.Output);
            param.Add("ApplicantEmail", dbType: DbType.String, direction: ParameterDirection.Output, size: 100);
            param.Add("HRManagerEmails", dbType: DbType.String, direction: ParameterDirection.Output, size: 500);
            param.Add("JobManagerEmails", dbType: DbType.String, direction: ParameterDirection.Output, size: 500);
            param.Add("JobTitle", dbType: DbType.String, direction: ParameterDirection.Output, size: 200);
            param.Add("CompanyName", dbType: DbType.String, direction: ParameterDirection.Output, size: 200);
            param.Add("OutJobID", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("InsertOrUpdateApplicantDataV12", param, commandType: CommandType.StoredProcedure);

            return (
                param.Get<int>("ApplicantID"),
                param.Get<string>("ApplicantEmail"),
                param.Get<string>("HRManagerEmails"),
                param.Get<string>("JobManagerEmails"),
                param.Get<string>("JobTitle"),
                param.Get<string>("CompanyName"),
                param.Get<int>("OutJobID")
            );
        }

        [HttpGet("applicant")]
        [TypeFilter(typeof(JwtAuthorizeAttribute))]
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
                _logger.LogError(ex, "Failed to retrieve applicants: {Message}", ex.Message);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("GetDataOpenFor")]
        [TypeFilter(typeof(JwtAuthorizeAttribute))]
        [ProducesResponseType(typeof(IEnumerable<dynamic>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDataOpenFor([FromQuery] string? Department = null)
        {
            try
            {
                using var connection = _context.CreateConnection();
                var parameters = new DynamicParameters();

                var query = "getDateOpenFor";
                parameters.Add("Department", Department);

                var response = await connection.QueryAsync(query,parameters);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve applicants: {Message}", ex.Message);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("applicantByID")]
        [TypeFilter(typeof(JwtAuthorizeAttribute))]
        [ProducesResponseType(typeof(IEnumerable<dynamic>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetApplicantsById([FromQuery] int? applicantId)
        {
            try
            {
                using var connection = _context.CreateConnection();
                var parameters = new DynamicParameters();

                parameters.Add($"@{ApplicantIdKey}", applicantId);

                var query = $"EXEC spGetAllApplicantsWithJobDetailsNew @{ApplicantIdKey}";
                var applicants = await connection.QueryAsync(query, parameters);

                return Ok(applicants);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve applicant by ID {ApplicantId}: {Message}", applicantId, ex.Message);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("addApplicant")]
        [TypeFilter(typeof(JwtAuthorizeAttribute))]
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
                _logger.LogError(ex, "Failed to insert applicant: {Message}", ex.Message);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("GetCandidate")]
        [TypeFilter(typeof(JwtAuthorizeAttribute))]
        [ProducesResponseType(typeof(IEnumerable<dynamic>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetFilteredCandidates([FromQuery] string? department, [FromQuery] int? jobId)
        {
            try
            {
                using var connection = _context.CreateConnection();

                var parameters = new DynamicParameters();
                parameters.Add("@Department", department);
                parameters.Add("@JobID", jobId);
                var result = await connection.QueryAsync(
                    "sp_GetCandidateAllV2",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve candidates for department {Department} and job ID {JobId}: {Message}", department, jobId, ex.Message);
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpGet("GetCandidateData")]
        [ProducesResponseType(typeof(IEnumerable<dynamic>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetApplicantData([FromQuery] int? id, int? JobId)
        {
            try
            {
                using var connection = _context.CreateConnection();

                var parameters = new DynamicParameters();
                parameters.Add($"@{ApplicantIdKey}", id);
                if (JobId != 0)
                {
                    parameters.Add($"@{JobIdKey}", JobId);
                }
                var result = await connection.QueryAsync(
                    "sp_GetApplicantDataV1",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve applicant data for ID {ApplicantId}: {Message}", id, ex.Message);
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpPut("updateApplicantStatus")]
        public async Task<IActionResult> UpdateApplicantStatus([FromBody] ExpandoObject? request)
        {
            try
            {
                if (request == null)
                {
                    _logger.LogWarning("Request is null in UpdateApplicantStatus");
                    return BadRequest("Request cannot be null.");
                }

                var data = (IDictionary<string, object?>)request;
                var validationResult = ValidateInput(data);
                if (validationResult != null)
                    return validationResult;

                var requestData = ExtractRequestData(data);
                if (requestData == null)
                    return BadRequest("Invalid ApplicantID or Status format.");

                var typeMail = requestData.TypeMail;

                if (typeMail == "Hire")
                {
                    await _emailNotificationService.SendHireToHrEmailsAsync(requestData);
                }
                else if (typeMail == "Selected")
                {
                    await _emailNotificationService.SendHrEmailsAsync(requestData);
                }
                else if (typeMail == "Confirmed")
                {
                    await _emailNotificationService.SendManagerEmailsAsync(requestData);
                }
                else if (typeMail == "notiMail")
                {
                    await _emailNotificationService.SendNotificationEmailsAsync(requestData);
                }

                if (typeMail != "notiMail")
                {
                    var hasRank = requestData.Candidates?.Any(c => c.RankOfSelect.HasValue) == true;

                    var updates = hasRank
                        ? requestData.Candidates!.Select(c => new ApplicantRequestData
                        {
                            ApplicantID = c.ApplicantID,
                            Status = typeMail == "Hire" ? requestData.Status : c.Status,
                            Remark = c.Remark,
                            RankOfSelect = c.RankOfSelect,
                            JobID = c.JobID
                        })
                        : [requestData];

                    foreach (var update in updates)
                    {
                        await UpdateStatusInDatabaseV2(update);
                    }
                }

                return Ok(new { message = "อัปเดตสถานะเรียบร้อย" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating applicant status: {Message}", ex.Message);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        private BadRequestObjectResult? ValidateInput(IDictionary<string, object?> data)
        {
            if (!data.ContainsKey(ApplicantIdKey) || !data.ContainsKey("Status"))
            {
                _logger.LogWarning("Missing required fields in request: ApplicantID or Status");
                return new BadRequestObjectResult("Missing required fields: ApplicantID or Status");
            }

            if (!data.TryGetValue(ApplicantIdKey, out object? applicantIdValue) || applicantIdValue == null ||
                !data.TryGetValue("Status", out object? statusValue) || statusValue == null)
            {
                _logger.LogWarning("Invalid or null values for ApplicantID or Status");
                return new BadRequestObjectResult("Invalid or null values for ApplicantID or Status");
            }

            return null;
        }

        private ApplicantRequestData? ExtractRequestData(IDictionary<string, object?> data)
        {
            if (data[ApplicantIdKey] is not JsonElement applicantIdElement || applicantIdElement.ValueKind != JsonValueKind.Number ||
                data["Status"] is not JsonElement statusElement || statusElement.ValueKind != JsonValueKind.String || data[JobIdKey] is not JsonElement jobIdElement)
            {
                _logger.LogWarning("ApplicantID must be an integer and Status must be a string");
                return null;
            }

            int ApplicantID = applicantIdElement.GetInt32();
            int JobID = jobIdElement.GetInt32();
            string status = statusElement.GetString()!;

            List<CandidateDto> candidates = ExtractCandidates(data);

            string? emailSend = data.TryGetValue("EmailSend", out object? emailSendObj) &&
                               emailSendObj is JsonElement emailSendElement &&
                               emailSendElement.ValueKind == JsonValueKind.String
                ? emailSendElement.GetString()
                : null;

            string requesterMail = data.TryGetValue("Email", out object? mailObj) ? mailObj?.ToString() ?? "-" : "-";
            string requesterName = data.TryGetValue("NAMETHAI", out object? nameObj) ? nameObj?.ToString() ?? "-" : "-";
            string requesterPost = data.TryGetValue("POST", out object? postObj) ? postObj?.ToString() ?? "-" : "-";
            string tel = data.TryGetValue("Mobile", out object? telObj) ? telObj?.ToString() ?? "-" : "-";
            string telOff = data.TryGetValue("TELOFF", out object? telOffObj) ? telOffObj?.ToString() ?? "-" : "-";
            string typeMail = data.TryGetValue("TypeMail", out object? typeMailObj) ? typeMailObj?.ToString() ?? "-" : "-";
            string department = data.TryGetValue("Department", out object? departmentObj) ? departmentObj?.ToString() ?? "-" : "-";
            string nameCon = data.TryGetValue("NameCon", out object? nameConObj) ? nameConObj?.ToString() ?? "-" : "-";
            string? remark = data.TryGetValue("Remark", out object? remarkObj) &&
                            remarkObj is JsonElement remarkElement &&
                            remarkElement.ValueKind == JsonValueKind.String
                ? remarkElement.GetString()
                : null;

            string jobTitle = data.TryGetValue(JobTitleKey, out object? jobTitleObj) &&
                              jobTitleObj is JsonElement jobTitleElement &&
                              jobTitleElement.ValueKind == JsonValueKind.String
                ? jobTitleElement.GetString() ?? "-"
                : "-";

            int? rankOfSelect = data.TryGetValue("RankOfSelect", out var rankObj) && int.TryParse(rankObj?.ToString(), out int RankOfSelect)
                ? RankOfSelect
                : (int?)null;
            return new ApplicantRequestData
            {
                ApplicantID = ApplicantID,
                Status = status,
                Candidates = candidates,
                EmailSend = emailSend,
                RequesterMail = requesterMail,
                RequesterName = requesterName,
                RequesterPost = requesterPost,
                Department = department,
                Tel = tel,
                TelOff = telOff,
                Remark = remark,
                JobTitle = jobTitle,
                TypeMail = typeMail,
                NameCon = nameCon,
                RankOfSelect = rankOfSelect,
                JobID = JobID
            };

        }

        private List<CandidateDto> ExtractCandidates(IDictionary<string, object?> data)
        {
            if (!data.TryGetValue("Candidates", out object? candidatesObj) || candidatesObj == null)
                return [];

            string? candidatesJson = candidatesObj.ToString();
            if (string.IsNullOrEmpty(candidatesJson))
                return [];

            try
            {
                return JsonSerializer.Deserialize<List<CandidateDto>>(candidatesJson) ?? [];
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize Candidates JSON: {Message}", ex.Message);
                return [];
            }
        }

        private async Task UpdateStatusInDatabase(int applicantId, string status)
        {
            using var connection = _context.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("@ApplicantID", applicantId);
            parameters.Add("@Status", status);

            await connection.ExecuteAsync(
                "sp_UpdateApplicantStatus",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        private async Task UpdateStatusInDatabaseV2(ApplicantRequestData requestData)
        {
            using var connection = _context.CreateConnection();
            var parameters = new DynamicParameters();

            parameters.Add("@ApplicantID", requestData.ApplicantID);
            parameters.Add("@Status", requestData.Status ?? "");
            parameters.Add("@JobID", requestData.JobID);
            if (!string.IsNullOrWhiteSpace(requestData.Remark))
            {
                parameters.Add("@Remark", requestData.Remark);
            }

            if (requestData.RankOfSelect != null)
            {
                parameters.Add("@RankOfSelect", requestData.RankOfSelect);
            }

            await connection.ExecuteAsync(
                "sp_UpdateApplicantStatusV3",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        [HttpPut("updateJobApprovalStatus")]
        [TypeFilter(typeof(JwtAuthorizeAttribute))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateJobApprovalStatus([FromBody] ExpandoObject? request)
        {
            try
            {
                if (request == null)
                {
                    _logger.LogWarning("Request is null in UpdateJobApprovalStatus");
                    return BadRequest("Request cannot be null.");
                }

                var data = (IDictionary<string, object?>)request;
                var validationResult = ValidateJobApprovalInput(data);
                if (validationResult != null)
                    return validationResult;

                var approvalData = ExtractJobApprovalData(data);
                if (approvalData == null)
                    return BadRequest("Invalid JobID or ApprovalStatus format.");

                await UpdateJobApprovalInDatabase(approvalData);

                return Ok(new { message = "อัปเดตสถานะของงานเรียบร้อย" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating job approval status: {Message}", ex.Message);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        private BadRequestObjectResult? ValidateJobApprovalInput(IDictionary<string, object?> data)
        {
            if (!data.ContainsKey(JobIdKey) || !data.ContainsKey("ApprovalStatus") || !data.ContainsKey("Remark"))
            {
                _logger.LogWarning("Missing required fields in request: JobID, ApprovalStatus, or Remark");
                return new BadRequestObjectResult("Missing required fields: JobID, ApprovalStatus, or Remark");
            }

            if (!data.TryGetValue(JobIdKey, out object? jobIdObj) || jobIdObj == null ||
                !data.TryGetValue("ApprovalStatus", out object? approvalStatusObj) || approvalStatusObj == null)
            {
                _logger.LogWarning("Invalid or null values for JobID or ApprovalStatus");
                return new BadRequestObjectResult("Invalid or null values for JobID or ApprovalStatus");
            }

            return null;
        }

        private JobApprovalData? ExtractJobApprovalData(IDictionary<string, object?> data)
        {
            if (data[JobIdKey] is not JsonElement jobIdElement || jobIdElement.ValueKind != JsonValueKind.Number ||
                data["ApprovalStatus"] is not JsonElement approvalStatusElement || approvalStatusElement.ValueKind != JsonValueKind.String)
            {
                _logger.LogWarning("JobID must be an integer and ApprovalStatus must be a string");
                return null;
            }

            int jobId = jobIdElement.GetInt32();
            string approvalStatus = approvalStatusElement.GetString()!;

            if (jobId == 0 || string.IsNullOrEmpty(approvalStatus))
            {
                _logger.LogWarning("JobID or ApprovalStatus cannot be null or invalid");
                return null;
            }

            string? remark = data.TryGetValue("Remark", out object? remarkObj) &&
                            remarkObj is JsonElement remarkElement &&
                            remarkElement.ValueKind == JsonValueKind.String
                ? remarkElement.GetString()
                : null;

            return new JobApprovalData(jobId, approvalStatus, remark);
        }

        private async Task UpdateJobApprovalInDatabase(JobApprovalData approvalData)
        {
            using var connection = _context.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("@JobID", approvalData.JobId);
            parameters.Add("@ApprovalStatus", approvalData.ApprovalStatus);
            parameters.Add("@Remark", approvalData.Remark != "" ? approvalData.Remark : "");

            try
            {
                var rowsAffected = await connection.ExecuteAsync(
                    "sp_UpdateJobApprovalStatus",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                if (rowsAffected != 0)
                {
                    await SendEmailsJobsStatusAsync(approvalData.JobId);
                }
                else
                {
                    _logger.LogWarning("sp_UpdateJobApprovalStatus: No rows were affected for JobId = {JobId}", approvalData.JobId);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }
        }
        
        private async Task<int> SendEmailsAsync(IEnumerable<string> recipients, string subject, string body)
        {
            int successCount = 0;
            foreach (var email in recipients)
            {
                if (string.IsNullOrWhiteSpace(email))
                    continue;

                try
                {
                    await _emailService.SendEmailAsync(email, subject, body, true, "Jobs",null);
                    successCount++;
                    _logger.LogInformation("Successfully sent email to {Email}", email);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send email to {Email}: {Message}", email, ex.Message);
                }
            }
            return successCount;
        }

        private async Task<int> SendEmailsJobsStatusAsync(int JobID)
        {
            using var connection = _context.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("@JobID", JobID);
            var result = await connection.QueryAsync<dynamic>(
                "sp_GetDataSendMailJobs @JobID",
                parameters);
            var emails = result
                .Select(r => ((string?)r?.EMAIL)?.Trim())
                .Where(email => !string.IsNullOrWhiteSpace(email))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var firstRecord = result.FirstOrDefault();
            string hrBody = string.Empty;
            string SubjectMail = string.Empty;
            hrBody = $@"
            <div style='font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px; font-size: 14px; line-height: 1.6;'>
                <p style='margin: 0;'>เรียนคุณ {firstRecord?.NAMETHAI} และคุณ {firstRecord?.ApproveNameThai},</p>

                {(firstRecord?.ApprovalStatus == "Approved" ? $@"
                    <p>
                        ฝ่ายทรัพยากรบุคคลได้ดำเนินการ <strong>อนุมัติ</strong> คำขอเปิดรับสมัครงานในตำแหน่ง 
                        <strong>{firstRecord?.JobTitle}</strong> เรียบร้อยแล้วค่ะ
                    </p>
                " : $@"
                    <p>
                        ฝ่ายทรัพยากรบุคคลได้ดำเนินการ <strong>ไม่อนุมัติ</strong> คำขอเปิดรับสมัครงานในตำแหน่ง 
                        <strong>{firstRecord?.JobTitle}</strong> ด้วยเหตุผลดังต่อไปนี้ค่ะ:
                    </p>
                    <blockquote style='background-color:#fff3f3; padding: 10px; border-left: 4px solid #ff4d4f;'>
                        <strong>{firstRecord?.Remark}</strong>
                    </blockquote>
                    <p>หากต้องการข้อมูลเพิ่มเติม กรุณาติดต่อฝ่ายทรัพยากรบุคคลโดยตรงค่ะ</p>
                ")}

                <p style='margin-top: 30px;'>ด้วยความเคารพ,</p>
                <p style='margin: 0;'>ฝ่ายทรัพยากรบุคคล</p>
                <br>
                <p style='color:red; font-weight: bold;'>**อีเมลนี้คือข้อความอัตโนมัติ กรุณาอย่าตอบกลับ**</p>
            </div>";
            SubjectMail = $@"แจ้งสถานะคำขอเปิดรับสมัครพนักงาน - ตำแหน่ง {firstRecord?.JobTitle}";
            return await SendEmailsAsync(emails!, SubjectMail, hrBody);
        }


        [HttpGet("GetPDPAContent")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
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
                _logger.LogError(ex, "Error retrieving PDPA content: {Message}", ex.Message);
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpPut("UpdateConfirmConsent")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateConfirmConsent([FromBody] ExpandoObject? request)
        {
            try
            {
                if (request == null)
                {
                    _logger.LogWarning("Request is null in UpdateConfirmConsent");
                    return BadRequest("Request cannot be null.");
                }

                var data = (IDictionary<string, object?>)request;
                var validationResult = ValidateConsentInput(data);
                if (validationResult != null)
                    return validationResult;

                var (userId, confirmConsent) = ExtractConsentData(data);
                if (userId == 0)
                    return BadRequest("Invalid UserId format.");
                if (string.IsNullOrEmpty(confirmConsent))
                    return BadRequest("ConfirmConsent cannot be null or empty.");

                using var connection = _context.CreateConnection();
                var parameters = new DynamicParameters();
                parameters.Add("@UserId", userId);
                parameters.Add("@ConfirmConsent", confirmConsent);
                var query = "EXEC UpdateUserConsent @UserId, @ConfirmConsent";
                var result = await connection.QuerySingleOrDefaultAsync<dynamic>(query, parameters);

                if (result == null)
                    return NotFound("User not found or update failed.");

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("GetCheckData")]
        [ProducesResponseType(typeof(IEnumerable<dynamic>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCheckData([FromQuery] string? Email, int? JobID)
        {
            try
            {
                using var connection = _context.CreateConnection();

                var parameters = new DynamicParameters();
                parameters.Add($"@Email", Email);
                parameters.Add($"@JobID", JobID);
                parameters.Add($"@UseBypass", true);
                var result = await connection.QueryAsync(
                    "sp_Userlogin",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve applicant data for ID {ApplicantId}: {Message}", JobID, ex.Message);
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpPost("delete-multiple-files")]
        public async Task<IActionResult> DeleteMultipleFiles([FromBody] List<int> fileIds)
        {
            try
            {
                using var connection = _context.CreateConnection();
                var jsonFileIds = JsonSerializer.Serialize(fileIds);

                var parameters = new { FileIDs = jsonFileIds };

                var deletedFiles = (await connection.QueryAsync<(int FileID, string FilePath)>(
                    "sp_DeleteApplicantFiles",
                    parameters,
                    commandType: CommandType.StoredProcedure
                )).ToList();

                foreach (var (FileID, FilePath) in deletedFiles)
                {
                    try
                    {
                        if (System.IO.File.Exists(FilePath))
                        {
                            System.IO.File.Delete(FilePath);
                            _logger.LogInformation("ลบไฟล์จริงสำเร็จ");
                        }
                        else
                        {
                            _logger.LogWarning("ไม่พบไฟล์จริง");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "ลบไฟล์จริงไม่สำเร็จ");
                    }
                }

                return Ok(new { success = true, message = "ลบไฟล์สำเร็จ" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ลบไฟล์ไม่สำเร็จ");
                return StatusCode(500, "เกิดข้อผิดพลาดในการลบไฟล์");
            }
        }


        [HttpPost("insertApplicant")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> InsertApplicant([FromForm] IFormCollection formData)
        {
            try
            {
                IDictionary<string, object?> req = new ExpandoObject();
                foreach (var key in formData.Keys)
                {
                    req[key] = formData[key].ToString();
                }
                string jsonInput = JsonSerializer.Serialize(req);
                string educationList = "[]";
                string workList = "[]";
                string skillsList = "[]";

                if (req.TryGetValue("EducationList", out var educationObj) && educationObj != null)
                {
                    educationList = educationObj.ToString() ?? "[]";
                }
                if (req.TryGetValue("WorkExperienceList", out var workObj) && workObj != null)
                {
                    workList = workObj.ToString() ?? "[]";
                }
                if (req.TryGetValue("SkillsList", out var skillsObj) && skillsObj != null)
                {
                    skillsList = skillsObj.ToString() ?? "[]";
                }
                var files = formData.Files;
                var fileMetadatas = await _fileProcessingService.ProcessFilesAsync(files);
                string filesList = JsonSerializer.Serialize(fileMetadatas);

                using var conn = _context.CreateConnection();
                var param = new DynamicParameters();
                param.Add("@JsonInput", jsonInput);
                param.Add("@EducationList", educationList);
                param.Add("@WorkExperienceList", workList);
                param.Add("@SkillsList", skillsList);
                param.Add("@FilesList", filesList);
                param.Add("@ApplicantID", dbType: DbType.Int32, direction: ParameterDirection.Output);

                await conn.ExecuteAsync("InsertApplicantDataRegister", param, commandType: CommandType.StoredProcedure);

                int applicantId = param.Get<int>("@ApplicantID");
                _fileProcessingService.MoveFilesToApplicantDirectory(applicantId, fileMetadatas);
                await _emailNotificationService.SendApplicationEmailsAsync(req, 
                    (applicantId, req["Email"]?.ToString() ?? "", "", "", req["JobTitle"]?.ToString() ?? "", req["CompanyName"]?.ToString() ?? "", req.TryGetValue("JobID", out object? value) ? Convert.ToInt32(value) : 0),
                    _applicationFormUri);

                return Ok(new
                {
                    Success = true,
                    ApplicantID = applicantId,
                    Message = "สมัครงานสำเร็จ"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error inserting applicant");
                return StatusCode(500, new { Success = false, Error = ex.Message });
            }
        }

        private BadRequestObjectResult? ValidateConsentInput(IDictionary<string, object?> data)
        {
            if (!data.ContainsKey("UserId") || !data.ContainsKey("confirmConsent"))
            {
                _logger.LogWarning("Missing required fields in request: UserId or ConfirmConsent");
                return BadRequest("Missing required fields: UserId or ConfirmConsent");
            }

            if (!data.TryGetValue("UserId", out var userIdObj) || userIdObj == null ||
                !data.TryGetValue("confirmConsent", out var _))
            {
                _logger.LogWarning("Invalid or null values for UserId or ConfirmConsent");
                return BadRequest("Invalid or null values for UserId or ConfirmConsent");
            }

            return null;
        }

        private static (int UserId, string? ConfirmConsent) ExtractConsentData(IDictionary<string, object?> data)
        {
            int userId = 0;
            string? confirmConsent = null;

            if (data["confirmConsent"] is JsonElement confirmConsentElement &&
                confirmConsentElement.ValueKind == JsonValueKind.String)
            {
                confirmConsent = confirmConsentElement.GetString() ?? string.Empty;
            }

            if (data["UserId"] is JsonElement userIdElement)
            {
                if (userIdElement.ValueKind == JsonValueKind.Number)
                {
                    userId = userIdElement.GetInt32();
                }
                else if (int.TryParse(userIdElement.GetString(), out var id))
                {
                    userId = id;
                }
            }

            return (userId, confirmConsent);
        }
    }
}