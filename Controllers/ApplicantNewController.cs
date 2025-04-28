using Dapper;
using Microsoft.AspNetCore.Mvc;
using JobOnlineAPI.DAL;
using System.Text.Json;
using JobOnlineAPI.Services;
using System.Dynamic;
using System.Data;
using System.Runtime.InteropServices;

namespace JobOnlineAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ApplicantNewController : ControllerBase
    {
        private readonly DapperContext _context;
        private readonly IEmailService _emailService;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ApplicantNewController> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string _basePath;
        private readonly string? _username;
        private readonly string? _password;
        private readonly bool _useNetworkShare;

        [DllImport("mpr.dll", EntryPoint = "WNetAddConnection2W", CharSet = CharSet.Unicode)]
        private static extern int WNetAddConnection2(ref NETRESOURCE netResource, string? password, string? username, int flags);

        [DllImport("mpr.dll", EntryPoint = "WNetCancelConnection2W", CharSet = CharSet.Unicode)]
        private static extern int WNetCancelConnection2(string lpName, int dwFlags, [MarshalAs(UnmanagedType.Bool)] bool fForce);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct NETRESOURCE
        {
            public int dwScope;
            public int dwType;
            public int dwDisplayType;
            public int dwUsage;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string? lpLocalName;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpRemoteName;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpComment;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string? lpProvider;
        }

        public ApplicantNewController(
            DapperContext context,
            IEmailService emailService,
            IWebHostEnvironment environment,
            IConfiguration configuration,
            ILogger<ApplicantNewController> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _emailService = emailService;
            _environment = environment;
            _configuration = configuration;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;

            string? environmentName = configuration["ASPNETCORE_ENVIRONMENT"] ?? "Development";
            string? origin = httpContextAccessor.HttpContext?.Request.Headers.Origin;
            string hostname = System.Net.Dns.GetHostName();
            _logger.LogInformation("Detected environment: {Environment}, Origin: {Origin}, Hostname: {Hostname}", environmentName, origin ?? "Not provided", hostname);

            bool isProduction = (origin != null && origin.Contains("10.10.0.27")) ||
                                environmentName.Equals("Production", StringComparison.OrdinalIgnoreCase);

            if (isProduction)
            {
                _basePath = @"C:\AppFiles\Applicants";
                _username = null;
                _password = null;
                _useNetworkShare = false;
            }
            else
            {
                _basePath = configuration.GetValue<string>("FileStorage:NetworkPath") ?? @"C:\AppFiles\Applicants";
                _username = configuration.GetValue<string>("FileStorage:Username");
                _password = configuration.GetValue<string>("FileStorage:Password");
                _useNetworkShare = configuration.GetValue<string>("FileStorage:NetworkPath") != null && _username != null && _password != null;
            }

            if (!_useNetworkShare && !Directory.Exists(_basePath))
            {
                Directory.CreateDirectory(_basePath);
                _logger.LogInformation("Created local directory: {BasePath}", _basePath);
            }
        }

        private async Task<bool> ConnectToNetworkShareAsync()
        {
            if (!_useNetworkShare)
            {
                if (Directory.Exists(_basePath))
                {
                    _logger.LogInformation("Using local storage at {BasePath}", _basePath);
                    return true;
                }
                _logger.LogError("Local path {BasePath} does not exist or is not accessible.", _basePath);
                throw new DirectoryNotFoundException($"Local path {_basePath} is not accessible.");
            }

            const int maxRetries = 3;
            const int retryDelayMs = 2000;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    string serverName = $"\\\\{new Uri(_basePath).Host}";
                    _logger.LogInformation("Attempt {Attempt}/{MaxRetries}: Disconnecting existing connections to {ServerName} and {BasePath}", attempt, maxRetries, serverName, _basePath);

                    int disconnectResult = WNetCancelConnection2(_basePath, 0, true);
                    if (disconnectResult != 0 && disconnectResult != 1219)
                    {
                        var errorMessage = new System.ComponentModel.Win32Exception(disconnectResult).Message;
                        _logger.LogWarning("Failed to disconnect existing connection to {BasePath}: {ErrorMessage} (Error Code: {DisconnectResult})", _basePath, errorMessage, disconnectResult);
                    }
                    else
                    {
                        _logger.LogInformation("Successfully disconnected or no existing connection to {BasePath} (Result: {DisconnectResult})", _basePath, disconnectResult);
                    }

                    disconnectResult = WNetCancelConnection2(serverName, 0, true);
                    if (disconnectResult != 0 && disconnectResult != 1219)
                    {
                        var errorMessage = new System.ComponentModel.Win32Exception(disconnectResult).Message;
                        _logger.LogWarning("Failed to disconnect existing connection to {ServerName}: {ErrorMessage} (Error Code: {DisconnectResult})", serverName, errorMessage, disconnectResult);
                    }
                    else
                    {
                        _logger.LogInformation("Successfully disconnected or no existing connection to {ServerName} (Result: {DisconnectResult})", serverName, disconnectResult);
                    }

                    NETRESOURCE netResource = new()
                    {
                        dwType = 1,
                        lpRemoteName = _basePath,
                        lpLocalName = null,
                        lpProvider = null
                    };

                    _logger.LogInformation("Attempt {Attempt}/{MaxRetries}: Connecting to network share {BasePath} with username {Username}", attempt, maxRetries, _basePath, _username);

                    int result = WNetAddConnection2(ref netResource, _password, _username, 0);
                    if (result != 0)
                    {
                        var errorMessage = new System.ComponentModel.Win32Exception(result).Message;
                        _logger.LogError("Failed to connect to network share {BasePath} with username {Username}: {ErrorMessage} (Error Code: {Result})", _basePath, _username, errorMessage, result);
                        if (result == 1219 && attempt < maxRetries)
                        {
                            _logger.LogInformation("Retrying connection after delay due to ERROR_SESSION_CREDENTIAL_CONFLICT (1219)");
                            await Task.Delay(retryDelayMs);
                            continue;
                        }
                        throw new System.ComponentModel.Win32Exception(result, $"Error connecting to network share: {errorMessage}");
                    }

                    if (!Directory.Exists(_basePath))
                    {
                        _logger.LogError("Network share {BasePath} does not exist or is not accessible.", _basePath);
                        throw new DirectoryNotFoundException($"Network share {_basePath} is not accessible.");
                    }

                    _logger.LogInformation("Successfully connected to network share: {BasePath}", _basePath);
                    return true;
                }
                catch (System.ComponentModel.Win32Exception win32Ex)
                {
                    _logger.LogError(win32Ex, "Attempt {Attempt}/{MaxRetries}: Win32 error connecting to network share {BasePath}: {Message}, ErrorCode: {ErrorCode}, StackTrace: {StackTrace}", attempt, maxRetries, _basePath, win32Ex.Message, win32Ex.NativeErrorCode, win32Ex.StackTrace);
                    if (win32Ex.NativeErrorCode == 1219 && attempt < maxRetries)
                    {
                        _logger.LogInformation("Retrying connection after delay due to ERROR_SESSION_CREDENTIAL_CONFLICT (1219)");
                        await Task.Delay(retryDelayMs);
                        continue;
                    }
                    throw new InvalidOperationException($"Cannot access network share {_basePath}: {win32Ex.Message}", win32Ex);
                }
                catch (DirectoryNotFoundException dirEx)
                {
                    _logger.LogError(dirEx, "Network share {BasePath} is not accessible: {Message}, StackTrace: {StackTrace}", _basePath, dirEx.Message, dirEx.StackTrace);
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Attempt {Attempt}/{MaxRetries}: Cannot access network share {BasePath} with username {Username}. StackTrace: {StackTrace}", attempt, maxRetries, _basePath, _username, ex.StackTrace);
                    if (attempt < maxRetries)
                    {
                        _logger.LogInformation("Retrying connection after delay");
                        await Task.Delay(retryDelayMs);
                        continue;
                    }
                    throw new InvalidOperationException($"Cannot access network share {_basePath}: {ex.Message}", ex);
                }
            }

            return false;
        }

        private void DisconnectFromNetworkShare()
        {
            if (!_useNetworkShare)
            {
                return;
            }

            try
            {
                string serverName = $"\\\\{new Uri(_basePath).Host}";
                _logger.LogInformation("Disconnecting from network share {BasePath} and server {ServerName}", _basePath, serverName);

                int disconnectResult = WNetCancelConnection2(_basePath, 0, true);
                if (disconnectResult != 0 && disconnectResult != 1219)
                {
                    var errorMessage = new System.ComponentModel.Win32Exception(disconnectResult).Message;
                    _logger.LogWarning("Failed to disconnect from {BasePath}: {ErrorMessage} (Error Code: {DisconnectResult})", _basePath, errorMessage, disconnectResult);
                }
                else
                {
                    _logger.LogInformation("Successfully disconnected or no existing connection to {BasePath} (Result: {DisconnectResult})", _basePath, disconnectResult);
                }

                disconnectResult = WNetCancelConnection2(serverName, 0, true);
                if (disconnectResult != 0 && disconnectResult != 1219)
                {
                    var errorMessage = new System.ComponentModel.Win32Exception(disconnectResult).Message;
                    _logger.LogWarning("Failed to disconnect from {ServerName}: {ErrorMessage} (Error Code: {DisconnectResult})", serverName, errorMessage, disconnectResult);
                }
                else
                {
                    _logger.LogInformation("Successfully disconnected or no existing connection to {ServerName} (Result: {DisconnectResult})", serverName, disconnectResult);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting from network share {BasePath}: {Message}, StackTrace: {StackTrace}", _basePath, ex.Message, ex.StackTrace);
            }
        }

        [HttpPost("submit-application-with-filesV2")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> SubmitApplicationWithFilesV2([FromForm] IFormFileCollection files, [FromForm] string jsonData)
        {
            try
            {
                if (string.IsNullOrEmpty(jsonData))
                {
                    return BadRequest("JSON data is required.");
                }

                var request = JsonSerializer.Deserialize<ExpandoObject>(jsonData);
                if (request is not IDictionary<string, object?> req || !req.TryGetValue("JobID", out var jobIdObj))
                {
                    return BadRequest("Invalid or missing JobID.");
                }

                int jobId = jobIdObj is JsonElement j && j.ValueKind == JsonValueKind.Number
                    ? j.GetInt32()
                    : Convert.ToInt32(jobIdObj);

                await ConnectToNetworkShareAsync();
                try
                {
                    var fileMetadatas = new List<Dictionary<string, object>>();

                    if (files != null && files.Count != 0)
                    {
                        foreach (var file in files)
                        {
                            if (file.Length == 0)
                            {
                                _logger.LogWarning("Skipping empty file: {FileName}", file.FileName);
                                continue;
                            }

                            var allowedExtensions = new[] { ".pdf", ".doc", ".docx",".png",".jpg" };
                            var extension = Path.GetExtension(file.FileName).ToLower();
                            if (!allowedExtensions.Contains(extension))
                            {
                                return BadRequest($"Invalid file type for {file.FileName}. Only PNG, JPG, PDF, DOC, and DOCX are allowed.");
                            }

                            var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                            var filePath = Path.Combine(_basePath, fileName);
                            var physicalPath = _useNetworkShare ? filePath : filePath;

                            var directoryPath = Path.GetDirectoryName(physicalPath);
                            if (!string.IsNullOrEmpty(directoryPath))
                            {
                                Directory.CreateDirectory(directoryPath);
                            }
                            else
                            {
                                _logger.LogError("Cannot create directory for path: {PhysicalPath}", physicalPath);
                                throw new InvalidOperationException($"Cannot create directory for path: {physicalPath}");
                            }

                            using (var stream = new FileStream(physicalPath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }

                            fileMetadatas.Add(new Dictionary<string, object>
                            {
                                { "FilePath", filePath.Replace('\\', '/') },
                                { "FileName", fileName },
                                { "FileSize", file.Length },
                                { "FileType", file.ContentType }
                            });
                        }
                    }

                    using var conn = _context.CreateConnection();
                    var param = new DynamicParameters();

                    string[] listKeys = ["EducationList", "WorkExperienceList", "SkillsList"];
                    foreach (var key in listKeys)
                    {
                        if (req.TryGetValue(key, out var val) && val is JsonElement je && je.ValueKind == JsonValueKind.Array)
                        {
                            param.Add(key, je.GetRawText());
                            req.Remove(key);
                        }
                        else
                        {
                            param.Add(key, "[]");
                        }
                    }

                    param.Add("JsonInput", JsonSerializer.Serialize(req));
                    param.Add("EducationList", param.Get<string>("EducationList"));
                    param.Add("WorkExperienceList", param.Get<string>("WorkExperienceList"));
                    param.Add("SkillsList", param.Get<string>("SkillsList"));
                    param.Add("FilesList", JsonSerializer.Serialize(fileMetadatas));
                    param.Add("JobID", jobId);
                    param.Add("ApplicantID", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    param.Add("ApplicantEmail", dbType: DbType.String, direction: ParameterDirection.Output, size: 100);
                    param.Add("HRManagerEmails", dbType: DbType.String, direction: ParameterDirection.Output, size: 500);
                    param.Add("JobManagerEmails", dbType: DbType.String, direction: ParameterDirection.Output, size: 500);
                    param.Add("JobTitle", dbType: DbType.String, direction: ParameterDirection.Output, size: 200);
                    param.Add("CompanyName", dbType: DbType.String, direction: ParameterDirection.Output, size: 200);

                    await conn.ExecuteAsync("InsertApplicantDataV6", param, commandType: CommandType.StoredProcedure);

                    var applicantId = param.Get<int>("ApplicantID");
                    var applicantEmail = param.Get<string>("ApplicantEmail");
                    var hrManagerEmails = param.Get<string>("HRManagerEmails");
                    var jobManagerEmails = param.Get<string>("JobManagerEmails");
                    var jobTitle = param.Get<string>("JobTitle");
                    var companyName = param.Get<string>("CompanyName");

                    if (fileMetadatas.Count != 0 && applicantId > 0)
                    {
                        var applicantPath = Path.Combine(_basePath, $"applicant_{applicantId}");
                        if (Directory.Exists(applicantPath))
                        {
                            foreach (var oldFile in Directory.GetFiles(applicantPath))
                            {
                                System.IO.File.Delete(oldFile);
                                _logger.LogInformation("Deleted old file in directory: {OldFile}", oldFile);
                            }
                        }
                        else
                        {
                            Directory.CreateDirectory(applicantPath);
                            _logger.LogInformation("Created applicant directory: {ApplicantPath}", applicantPath);
                        }

                        foreach (var metadata in fileMetadatas)
                        {
                            var oldFilePath = metadata["FilePath"]?.ToString();
                            if (string.IsNullOrEmpty(oldFilePath))
                            {
                                _logger.LogWarning("Skipping file with null or empty FilePath in metadata: {Metadata}", JsonSerializer.Serialize(metadata));
                                continue;
                            }

                            var fileName = metadata["FileName"]?.ToString();
                            if (string.IsNullOrEmpty(fileName))
                            {
                                _logger.LogWarning("Skipping file with invalid FileName in metadata: {Metadata}", JsonSerializer.Serialize(metadata));
                                continue;
                            }

                            var newFilePath = Path.Combine(_basePath, $"applicant_{applicantId}", fileName);
                            var newPhysicalPath = _useNetworkShare ? newFilePath : newFilePath;
                            var oldPhysicalPath = _useNetworkShare ? oldFilePath : oldFilePath;

                            if (System.IO.File.Exists(oldPhysicalPath))
                            {
                                System.IO.File.Move(oldPhysicalPath, newPhysicalPath, overwrite: true);
                                _logger.LogInformation("Moved file from {OldPhysicalPath} to {NewPhysicalPath}", oldPhysicalPath, newPhysicalPath);
                            }
                            else
                            {
                                _logger.LogWarning("File not found for moving: {OldPhysicalPath}", oldPhysicalPath);
                                continue;
                            }
                        }
                    }

                    req.TryGetValue("FirstNameThai", out var firstNameThaiObj);
                    req.TryGetValue("LastNameThai", out var lastNameThaiObj);
                    req.TryGetValue("FirstNameEng", out var firstNameEngObj);
                    req.TryGetValue("LastNameEng", out var lastNameEngObj);

                    var fullNameThai = $"{firstNameThaiObj?.ToString() ?? ""} {lastNameThaiObj?.ToString() ?? ""}".Trim();
                    var fullNameEng = $"{firstNameEngObj?.ToString() ?? ""} {lastNameEngObj?.ToString() ?? ""}".Trim();
                    // var JobTitle = param.Get<string>("JobTitle");
                    var dict = (IDictionary<string, object>)request;
                    string JobTitle = dict["JobTitle"]?.ToString() ?? "-";
                    var results = await conn.QueryAsync<dynamic>(
                        "sp_GetDateSendEmailV3",
                        new { JobID = jobId },
                        commandType: CommandType.StoredProcedure
                    );
                    var firstHr = results.FirstOrDefault(x => Convert.ToInt32(x.Role) == 2);
                    string Tel = firstHr?.TELOFF ?? "-";
                    string resultMail = firstHr?.EMAIL ?? "-";
                    string CompanyName = firstHr?.COMPANY_NAME ?? "-";
                    string POST = firstHr?.POST ?? "-";
                    string hrName = firstHr?.NAMETHAI ?? "-";
                    
                    if (!string.IsNullOrEmpty(applicantEmail))
                    {
                        string applicantBody = $@"
                        <div style='font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px; font-size: 14px; line-height: 1.6;'>
                            <p style='margin: 0; font-weight: bold;'>{CompanyName}: ได้รับใบสมัครงานของคุณแล้ว</p>
                            <p style='margin: 0;'>เรียน คุณ {fullNameThai}</p>

                            <p>
                                ขอบคุณสำหรับความสนใจในตำแหน่ง <strong>{JobTitle}</strong> ที่บริษัท <strong>{CompanyName}</strong> ของเรา<br>
                                เราขอยืนยันว่าได้รับใบสมัครของท่านเรียบร้อยแล้ว ทีมงานฝ่ายทรัพยากรบุคคลของเรากำลังพิจารณาใบสมัครของท่าน และจะติดต่อกลับภายใน 7-14 วันทำการ หากคุณสมบัติของท่านตรงตามที่เรากำลังมองหา<br><br>
                                หากท่านมีข้อสงสัยหรือต้องการข้อมูลเพิ่มเติม สามารถติดต่อเราได้ที่อีเมล 
                                <span style='color: blue;'>{resultMail}</span> หรือโทร 
                                <span style='color: blue;'>{Tel}</span><br>
                                ขอบคุณอีกครั้งสำหรับความสนใจร่วมงานกับเรา
                            </p>

                            <p style='margin-top: 30px; margin:0'>ด้วยความเคารพ,</p>
                            <p style='margin: 0;'>{hrName}</p>
                            <p style='margin: 0;'>ฝ่ายทรัพยากรบุคคล</p>
                            <p style='margin: 0;'>{CompanyName}</p>
                            <br>

                            <p style='color:red; font-weight: bold;'>**อีเมลนี้คือข้อความอัตโนมัติ กรุณาอย่าตอบกลับ**</p>
                        </div>";
                        await _emailService.SendEmailAsync(applicantEmail, "Application Received", applicantBody, true);
                    }

                    foreach (var x in results)
                    {
                        var emailStaff = (x.EMAIL ?? "").ToString();
                        var posterName = (x.NAMETHAI ?? "-").ToString();
                        var postName = (x.POST ?? "-").ToString();

                        if (!string.IsNullOrWhiteSpace(emailStaff))
                        {
                            string managerBody = $@"
                            <div style='font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px; font-size: 14px; line-height: 1.6;'>
                                <p style='margin: 0;'>เรียนทุกท่าน</p>
                                <p style='margin: 0;'>เรื่อง: แจ้งข้อมูลผู้สมัครตำแหน่ง <strong>{JobTitle}</strong></p>

                                <p style='margin: 0;'>ทางฝ่ายรับสมัครงานขอแจ้งให้ทราบว่า คุณ <strong>{fullNameThai}</strong> ได้ทำการสมัครงานเข้ามาในตำแหน่ง <strong>{JobTitle}</strong></p>

                                <p style='margin: 0;'>กรุณาคลิก Link:
                                    <a target='_blank' href='https://oneejobs.oneeclick.co:7191/ApplicationForm/ApplicationFormView?id={applicantId}' 
                                    style='color: #007bff; text-decoration: underline;'>
                                    https://oneejobs.oneeclick.co
                                    </a>
                                    เพื่อดูรายละเอียดและดำเนินการในขั้นตอนต่อไป
                                </p>

                                <br>

                                <p style='color: red; font-weight: bold;'>**อีเมลนี้คือข้อความอัตโนมัติ กรุณาอย่าตอบกลับ**</p>
                            </div>";
                            await _emailService.SendEmailAsync(emailStaff.Trim(), "ONEE Jobs - You've got the new candidate", managerBody, true);
                        }
                    }
                    return Ok(new { ApplicantID = applicantId, Message = "Application and files submitted successfully." });
                }
                finally
                {
                    DisconnectFromNetworkShare();
                }
            }
            catch (System.ComponentModel.Win32Exception win32Ex)
            {
                _logger.LogError(win32Ex, "Win32 error processing application with files: {Message}, ErrorCode: {ErrorCode}, StackTrace: {StackTrace}", win32Ex.Message, win32Ex.NativeErrorCode, win32Ex.StackTrace);
                return StatusCode(500, new { Error = "Server error", win32Ex.Message, ErrorCode = win32Ex.NativeErrorCode });
            }
            catch (DirectoryNotFoundException dirEx)
            {
                _logger.LogError(dirEx, "Network share not accessible: {Message}, StackTrace: {StackTrace}", dirEx.Message, dirEx.StackTrace);
                return StatusCode(500, new { Error = "Server error", dirEx.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing application with files: {Message}, StackTrace: {StackTrace}", ex.Message, ex.StackTrace);
                return StatusCode(500, new { Error = "Server error", ex.Message });
            }
        }
  
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

        [HttpGet("GetCandidate")]
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
        [ProducesResponseType(typeof(IEnumerable<dynamic>), StatusCodes.Status200OK)]
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

                var candidatesJson = data.ContainsKey("Candidates") ? data["Candidates"].ToString() : null;
                List<ExpandoObject> candidates = !string.IsNullOrEmpty(candidatesJson)
                ? JsonSerializer.Deserialize<List<ExpandoObject>>(candidatesJson)
                                    : new List<ExpandoObject>();

                var EmailSend = data.ContainsKey("EmailSend") ? ((JsonElement)data["EmailSend"]).GetString() : null;

                var requesterMail = ((JsonElement)data["Email"]).GetString() ?? "-";
                var requesterName = ((JsonElement)data["NAMETHAI"]).GetString() ?? "-";
                var Tel = ((JsonElement)data["Mobile"]).GetString() ?? "-";
                var requesterPost = ((JsonElement)data["POST"]).GetString() ?? "-";
                var TelOff = ((JsonElement)data["TELOFF"]).GetString() ?? "-";
                var JobTitle = ((JsonElement)data["JobTitle"]).GetString() ?? "-";

                parameters.Add("@ApplicantID", applicantId);
                parameters.Add("@Status", status);

                var query = "EXEC sp_UpdateApplicantStatus @ApplicantID, @Status";
                await connection.ExecuteAsync(query, parameters);

                var candidateNames = candidates.Select(candidateObj =>
                {
                    var candidateDict = candidateObj as IDictionary<string, object>;
                    var title = candidateDict.ContainsKey("title") ? candidateDict["title"].ToString() : "";
                    var firstNameThai = candidateDict.ContainsKey("firstNameThai") ? candidateDict["firstNameThai"].ToString() : "";
                    var lastNameThai = candidateDict.ContainsKey("lastNameThai") ? candidateDict["lastNameThai"].ToString() : "";
                    return $"{title} {firstNameThai} {lastNameThai}".Trim();
                }).ToList();

                
                var candidateNamesString = string.Join(" ", candidateNames);

                string hrBody = $@"
                    <div style='font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px; font-size: 14px;'>
                        <p style='font-weight: bold; margin: 0 0 10px 0;'>เรียน คุณสมศรี (ผู้จัดการฝ่ายบุคคล)</p>
                        <p style='font-weight: bold; margin: 0 0 10px 0;'>เรื่อง: การเรียกสัมภาษณ์ผู้สมัครตำแหน่ง {JobTitle}</p>
                        <br>
                        <p style='margin: 0 0 10px 0;'>
                            เรียน ฝ่ายบุคคล<br>
                            ตามที่ได้รับแจ้งข้อมูลผู้สมัครในตำแหน่ง {JobTitle} จำนวน {candidates.Count} ท่าน ผมได้พิจารณาประวัติและคุณสมบัติเบื้องต้นแล้ว และประสงค์จะขอเรียกผู้สมัครดังต่อไปนี้เข้ามาสัมภาษณ์
                        </p>
                        <p style='margin: 0 0 10px 0;'>
                            จากข้อมูลผู้สมัคร ดิฉัน/ผมเห็นว่า {candidateNamesString} มีคุณสมบัติที่เหมาะสมกับตำแหน่งงาน และมีความเชี่ยวชาญในทักษะที่จำเป็นต่อการทำงานในทีมของเรา
                        </p>
                        <br>
                        <p style='margin: 0 0 10px 0;'>ขอความกรุณาฝ่ายบุคคลประสานงานกับผู้สมัครเพื่อนัดหมายการสัมภาษณ์</p>
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
                            await _emailService.SendEmailAsync(hrEmail, "ONEE Jobs - List of candidates for job interview", hrBody, true);
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
                if (!data.TryGetValue("JobID", out var jobIdObj) || !data.TryGetValue("ApprovalStatus", out var approvalStatusObj) || !data.TryGetValue("Remark", out var RemarkObj))
                    return BadRequest("Missing required fields: JobID or ApprovalStatus");

                var jobId = ((JsonElement)jobIdObj).GetInt32();
                var approvalStatus = ((JsonElement)approvalStatusObj).GetString();
                var Remark = ((JsonElement)RemarkObj).GetString();

                if (approvalStatus == null)
                    return BadRequest("ApprovalStatus cannot be null.");

                parameters.Add("@JobID", jobId);
                parameters.Add("@ApprovalStatus", approvalStatus);
                parameters.Add("@Remark", Remark);

                var query = "EXEC sp_UpdateJobApprovalStatus @JobID, @ApprovalStatus, @Remark";
                await connection.ExecuteAsync(query, parameters);

                return Ok(new { message = "อัปเดตสถานะของงานเรียบร้อย" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
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
                return StatusCode(500, new { Error = ex.Message });
            }
        }
    }
}