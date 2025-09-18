using System.Text.Json;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using JobOnlineAPI.Services;
using Rotativa.AspNetCore;
using System.Dynamic;
using JobOnlineAPI.Filters;

namespace JobOnlineAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ITRequestsController(IConfiguration configuration, IEmailService emailService, ILogger<ITRequestsController> logger) : ControllerBase
    {
        private readonly IDbConnection _dbConnection = new SqlConnection(configuration.GetConnectionString("DefaultConnection"));
        private readonly IEmailService _emailService = emailService;
        private readonly ILogger<ITRequestsController> _logger = logger;
        private readonly string _defaultEmail = "default@company.com";

        private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
        {
            WriteIndented = false,

        };

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SubmitITRequest([FromForm] IFormCollection formCollection)
        {
            try
            {
                // ดึงข้อมูล JSON และ clean string
                string? jsonData = formCollection["jsonData"].FirstOrDefault()?.Trim();
                if (string.IsNullOrWhiteSpace(jsonData) || jsonData == "[]")
                {
                    _logger.LogWarning("Invalid or missing JSON data.");
                    return BadRequest(new { Error = "Invalid or missing JSON data." });
                }

                // Validate JSON
                JsonDocument request;
                try
                {
                    request = JsonDocument.Parse(jsonData);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning("Invalid JSON format: {Message}", ex.Message);
                    return BadRequest(new { Error = $"Invalid JSON format: {ex.Message}" });
                }

                if (request.RootElement.ValueKind != JsonValueKind.Object && request.RootElement.ValueKind != JsonValueKind.Array)
                {
                    _logger.LogWarning("Invalid JSON format. Expected object or array.");
                    return BadRequest(new { Error = "Invalid JSON format. Expected a JSON object or array." });
                }

                List<Dictionary<string, object>> requestDataList = [];
                string? createdBy = null;
                string? jsonReqNo = null;
                bool isArray = request.RootElement.ValueKind == JsonValueKind.Array;

                // Convert uploaded files to base64 for signatures
                var ITRequesterOld = formCollection["requesterSignatureFile"];
                var ITOfficerOld = formCollection["itOfficerSignatureFile"];
                var signatures = new Dictionary<string, string?>
                {
                    ["RequesterSignature"] = await ConvertFileToBase64(formCollection.Files["requesterSignatureFile"]),
                    ["ApproverSignature"] = await ConvertFileToBase64(formCollection.Files["approverSignatureFile"]),
                    ["UATUserSignature"] = await ConvertFileToBase64(formCollection.Files["uatUserSignatureFile"]),
                    ["ITOfficerSignature"] = await ConvertFileToBase64(formCollection.Files["itOfficerSignatureFile"]),
                    ["OtherApproverSignature"] = await ConvertFileToBase64(formCollection.Files["otherApproverSignatureFile"]),
                    ["OtherUATUserSignature"] = await ConvertFileToBase64(formCollection.Files["otherUatUserSignatureFile"])
                };

                // ดึง signatureId จาก form
                string? signatureIdValue = formCollection["signatureId"].FirstOrDefault();
                int? parsedSignatureId = signatureIdValue != null && int.TryParse(signatureIdValue, out int tempSignatureId) ? tempSignatureId : (int?)null;

                // Handle single JSON object
                if (!isArray)
                {
                    if (!request.RootElement.TryGetProperty("CreatedBy", out var createdByElement) || createdByElement.ValueKind == JsonValueKind.Null || string.IsNullOrWhiteSpace(createdByElement.GetString()))
                    {
                        _logger.LogWarning("CreatedBy is required and cannot be empty.");
                        return BadRequest(new { Error = "CreatedBy is required and cannot be empty." });
                    }

                    createdBy = createdByElement.GetString()!;
                    var requestData = new Dictionary<string, object>();
                    foreach (var property in request.RootElement.EnumerateObject())
                    {
                        if (property.Name.Equals("REQ_NO", StringComparison.OrdinalIgnoreCase))
                        {
                            jsonReqNo = property.Value.GetString();
                        }
                        requestData[property.Name] = property.Value.ValueKind switch
                        {
                            JsonValueKind.String => property.Value.GetString() ?? string.Empty,
                            JsonValueKind.Number => property.Value.TryGetInt32(out int intValue) ? intValue : property.Value.GetDouble(),
                            JsonValueKind.True => true,
                            JsonValueKind.False => false,
                            JsonValueKind.Null => string.Empty,
                            JsonValueKind.Undefined => throw new NotSupportedException("Undefined JSON value not supported"),
                            JsonValueKind.Object => throw new NotSupportedException("Nested JSON object not supported"),
                            JsonValueKind.Array => throw new NotSupportedException("Nested JSON array not supported"),
                            _ => property.Value.ToString() ?? string.Empty
                        };
                    }
                    requestDataList.Add(requestData);
                }
                // Handle JSON array
                else
                {
                    foreach (var item in request.RootElement.EnumerateArray())
                    {
                        if (item.ValueKind != JsonValueKind.Object)
                        {
                            _logger.LogWarning("Each item in the array must be a JSON object.");
                            return BadRequest(new { Error = "Each item in the array must be a JSON object." });
                        }

                        if (!item.TryGetProperty("CreatedBy", out var createdByElement) || createdByElement.ValueKind == JsonValueKind.Null || string.IsNullOrWhiteSpace(createdByElement.GetString()))
                        {
                            _logger.LogWarning("CreatedBy is required and cannot be empty in each array item.");
                            return BadRequest(new { Error = "CreatedBy is required and cannot be empty in each array item." });
                        }

                        createdBy ??= createdByElement.GetString()!;

                        var requestData = new Dictionary<string, object>();
                        foreach (var property in item.EnumerateObject())
                        {
                            if (property.Name.Equals("REQ_NO", StringComparison.OrdinalIgnoreCase))
                            {
                                string? currentReqNo = property.Value.GetString();
                                if (currentReqNo != null)
                                {
                                    if (jsonReqNo == null)
                                        jsonReqNo = currentReqNo;
                                    else if (jsonReqNo != currentReqNo)
                                    {
                                        _logger.LogWarning("All items in the array must have the same REQ_NO value.");
                                        return BadRequest(new { Error = "All items in the array must have the same REQ_NO value." });
                                    }
                                }
                            }

                            requestData[property.Name] = property.Value.ValueKind switch
                            {
                                JsonValueKind.String => property.Value.GetString() ?? string.Empty,
                                JsonValueKind.Number => property.Value.TryGetInt32(out int intValue) ? intValue : property.Value.GetDouble(),
                                JsonValueKind.True => true,
                                JsonValueKind.False => false,
                                JsonValueKind.Null => string.Empty,
                                JsonValueKind.Undefined => throw new NotSupportedException("Undefined JSON value not supported"),
                                JsonValueKind.Object => throw new NotSupportedException("Nested JSON object not supported"),
                                JsonValueKind.Array => throw new NotSupportedException("Nested JSON array not supported"),
                                _ => property.Value.ToString() ?? string.Empty
                            };
                        }
                        requestDataList.Add(requestData);
                    }
                }
                var requesterName = Request.Form["RequesterName"].ToString().Split(',').FirstOrDefault()?.Trim() ?? "";
                var approverName = Request.Form["ApproverName"].ToString().Split(',').FirstOrDefault()?.Trim() ?? "";
                var uatUserName = Request.Form["UATUserName"].ToString().Split(',').FirstOrDefault()?.Trim() ?? "";
                var itOfficerName = Request.Form["ITOfficerName"].ToString().Split(',').FirstOrDefault()?.Trim() ?? "";
                var otherApproverName = Request.Form["OtherApproverName"].ToString().Split(',').FirstOrDefault()?.Trim() ?? "";
                var otherUatUserName = Request.Form["OtherUATUserName"].ToString().Split(',').FirstOrDefault()?.Trim() ?? "";


                var IT_ACK_DATE = Request.Form["IT_ACK_DATE"].ToString().Split(',').FirstOrDefault()?.Trim() ?? "";
                var IT_PIC = Request.Form["IT_PIC"].ToString().Split(',').FirstOrDefault()?.Trim() ?? "";
                var IT_COMMENT = Request.Form["IT_COMMENT"].ToString().Split(',').FirstOrDefault()?.Trim() ?? "";


                var NAMETHAI = Request.Form["NAMETHAI"].ToString().Split(',').FirstOrDefault()?.Trim() ?? "";
                var NAMECOSTCENT = Request.Form["NAMECOSTCENT"].ToString().Split(',').FirstOrDefault()?.Trim() ?? "";
                var TypeSendEMAIL = Request.Form["TypeSendEMAIL"].ToString().Split(',').FirstOrDefault()?.Trim() ?? "";


                using var connection = new SqlConnection(_dbConnection.ConnectionString);
                var parameters = new DynamicParameters();
                string serializedJsonData = isArray ? JsonSerializer.Serialize(requestDataList, _jsonSerializerOptions) : JsonSerializer.Serialize(requestDataList[0], _jsonSerializerOptions);
                parameters.Add("JsonData", serializedJsonData);
                parameters.Add("CreatedBy", createdBy);
                parameters.Add("ReqNo", jsonReqNo);
                parameters.Add("IT_ACK_DATE", IT_ACK_DATE);
                parameters.Add("IT_PIC", IT_PIC);
                parameters.Add("IT_COMMENT", IT_COMMENT);

                parameters.Add("RequesterSignature", signatures["RequesterSignature"]);
                parameters.Add("ApproverSignature", signatures["ApproverSignature"]);
                parameters.Add("UATUserSignature", signatures["UATUserSignature"]);
                parameters.Add("ITOfficerSignature", signatures["ITOfficerSignature"]);
                parameters.Add("OtherApproverSignature", signatures["OtherApproverSignature"]);
                parameters.Add("OtherUATUserSignature", signatures["OtherUATUserSignature"]);
                parameters.Add("SignatureID", parsedSignatureId, DbType.Int32);
                parameters.Add("RequesterCode", requesterName);
                parameters.Add("ApproverName", approverName);
                parameters.Add("UATUserName", uatUserName);
                parameters.Add("ITOfficerName", itOfficerName);
                parameters.Add("OtherApproverName", otherApproverName);
                parameters.Add("OtherUATUserName", otherUatUserName);
                parameters.Add("NewID", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("ErrorMessage", dbType: DbType.String, direction: ParameterDirection.Output, size: 500);

                var result = await connection.QueryAsync(
                    "usp_DynamicInsertUpdateT_EMP_IT_REQV2",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var newId = parameters.Get<int?>("NewID");
                var errorMessage = parameters.Get<string>("ErrorMessage");

                if (!string.IsNullOrEmpty(errorMessage))
                {
                    _logger.LogError("Error from stored procedure: {ErrorMessage}", errorMessage);
                    return BadRequest(new { Error = errorMessage });
                }

                if (newId == null)
                {
                    _logger.LogError("NewID is null after stored procedure execution.");
                    return StatusCode(500, new { Error = "Failed to process IT request." });
                }

                // Create response from result set with adjusted signatures
                int index = 0;
                var responseItems = result.Select(r =>
                {
                    var requestData = isArray && index < requestDataList.Count ? requestDataList[index++] : requestDataList.FirstOrDefault();
                    return new
                    {
                        ITRequestId = r.NewID,
                        r.ReqNo,
                        Message = requestData != null && requestData.ContainsKey("ID") ? "IT request updated successfully." : "IT request created successfully.",
                        FilePath = requestData != null && requestData.TryGetValue("FilePath", out var filePath) && filePath != null ? filePath.ToString() : null,
                        RequesterSignature = signatures["RequesterSignature"] ?? (r.RequesterSignature as string),
                        ApproverSignature = signatures["ApproverSignature"] ?? (r.ApproverSignature as string),
                        UATUserSignature = signatures["UATUserSignature"] ?? (r.UATUserSignature as string),
                        ITOfficerSignature = signatures["ITOfficerSignature"] ?? (r.ITOfficerSignature as string),
                        OtherApproverSignature = signatures["OtherApproverSignature"] ?? (r.OtherApproverSignature as string),
                        OtherUATUserSignature = signatures["OtherUATUserSignature"] ?? (r.OtherUATUserSignature as string)
                    };
                }).ToList();

                // Send a single email for the entire request with signatures
                if (TypeSendEMAIL != "" && TypeSendEMAIL != null)
                {
                    var firstJson = JsonSerializer.Serialize(requestDataList[0]);
                    dynamic? firstObj = JsonSerializer.Deserialize<ExpandoObject>(firstJson);
                    int applicantId = 0;
                    if (firstObj is ExpandoObject expando)
                    {
                        var dict = (IDictionary<string, object?>)expando;

                        if (dict.TryGetValue("ApplicantID", out var value) && value is JsonElement jsonElement)
                        {
                            if (jsonElement.ValueKind == JsonValueKind.Number && jsonElement.TryGetInt32(out int parsed))
                            {
                                applicantId = parsed;
                            }
                        }
                    }
                    await SendEmailsITAsync(applicantId, TypeSendEMAIL, NAMETHAI, NAMECOSTCENT);
                }


                //var firstResult = result.FirstOrDefault();
                //if (firstResult != null)
                //{
                //    string? reqNo = firstResult.ReqNo;
                //    string? approver1 = firstResult.APPROVER1;
                //    string? approver2 = firstResult.APPROVER2;
                //    string? approver3 = firstResult.APPROVER3;
                //    string? approver4 = firstResult.APPROVER4;
                //    string? approver5 = firstResult.APPROVER5;
                //    bool isUpdate = requestDataList.Any(data => data.ContainsKey("ID"));
                //    await SendITRequestEmail(requestDataList, newId.Value, isUpdate, reqNo, approver1, approver2, approver3, approver4, approver5);
                //}

                return Ok(new
                {
                    ITRequests = responseItems,
                    Message = requestDataList.Any(data => data.ContainsKey("ID")) ? "IT requests updated successfully." : "Multiple IT requests created successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in SubmitITRequest: {Message}", ex.Message);
                return StatusCode(500, new { Error = "Internal Server Error", Details = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetITRequestByReqNo(string? reqNo = null, int? applicantId = null)
        {
            try
            {
                using var connection = new SqlConnection(_dbConnection.ConnectionString);
                var parameters = new DynamicParameters();
                parameters.Add("REQ_NO", string.IsNullOrWhiteSpace(reqNo) ? null : reqNo);
                parameters.Add("ApplicantID", applicantId);
                parameters.Add("ErrorMessage", dbType: DbType.String, direction: ParameterDirection.Output, size: 500);
                using var multi = await connection.QueryMultipleAsync(
                    "usp_GetT_EMP_IT_REQ_ByReqNoV3",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var itRequests = multi.Read<dynamic>().ToList();
                var servicesList = multi.IsConsumed ? [] : multi.Read<dynamic>().Select(s =>
                {
                    var dict = new Dictionary<string, object>();
                    foreach (var prop in ((IDictionary<string, object>)s))
                    {
                        dict[prop.Key] = prop.Value;
                    }
                    return dict;
                }).ToList();
                var signaturesList = multi.IsConsumed ? [] : multi.Read<dynamic>().Select(sig =>
                {
                    var dict = new Dictionary<string, object>();
                    foreach (var prop in ((IDictionary<string, object>)sig))
                    {
                        dict[prop.Key] = prop.Value;
                    }
                    return dict;
                }).ToList();

                var errorMessage = parameters.Get<string>("ErrorMessage");

                if (!string.IsNullOrEmpty(errorMessage))
                {
                    return BadRequest(new { Error = errorMessage });
                }

                return Ok(new
                {
                    ITRequests = itRequests,
                    Services = servicesList,
                    Signatures = signaturesList,
                    Message = itRequests.Count == 0 ? "No IT requests found." : "IT requests, services, and signatures retrieved successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in GetITRequestByReqNo: {Message}", ex.Message);
                return StatusCode(500, new { Error = "Internal Server Error", Details = ex.Message });
            }
        }

        [HttpGet("generate-form")]
        public async Task<IActionResult> GenerateForm(string? reqNo = null, int? applicantId = null)
        {
            try
            {
                using var connection = new SqlConnection(_dbConnection.ConnectionString);
                var parameters = new DynamicParameters();
                parameters.Add("REQ_NO", string.IsNullOrWhiteSpace(reqNo) ? null : reqNo);
                parameters.Add("ApplicantID", applicantId);
                parameters.Add("ErrorMessage", dbType: DbType.String, direction: ParameterDirection.Output, size: 500);

                using var multi = await connection.QueryMultipleAsync(
                    "usp_GetT_EMP_IT_REQ_ByReqNoV2",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var itRequests = multi.Read<dynamic>().ToList();
                var servicesList = multi.IsConsumed ? [] : multi.Read<dynamic>().Select(s =>
                {
                    var dict = new Dictionary<string, object>();
                    foreach (var prop in ((IDictionary<string, object>)s))
                    {
                        dict[prop.Key] = prop.Value;
                    }
                    return dict;
                }).ToList();
                var signaturesList = multi.IsConsumed ? [] : multi.Read<dynamic>().Select(sig =>
                {
                    var dict = new Dictionary<string, object>();
                    foreach (var prop in ((IDictionary<string, object>)sig))
                    {
                        dict[prop.Key] = prop.Value;
                    }
                    return dict;
                }).ToList();

                var errorMessage = parameters.Get<string>("ErrorMessage");

                if (!string.IsNullOrEmpty(errorMessage))
                {
                    return BadRequest(new { Error = errorMessage });
                }

                var firstResult = itRequests.FirstOrDefault();
                if (firstResult == null)
                {
                    return NotFound(new { Message = $"No IT requests found for REQ_NO: {reqNo}, ApplicantID: {applicantId}" });
                }

                var firstSignature = signaturesList.FirstOrDefault();

                var dataDict = new Dictionary<string, object>
                {
                    ["FormNumber"] = firstResult.REQ_NO ?? reqNo ?? "N/A",
                    ["RequestDate"] = firstResult.REQ_DATE is DateTime date ? date : DateTime.Now,
                    ["Company"] = firstResult.Company ?? "Example Company Ltd.",
                    ["CostCenter"] = firstResult.CostCenter ?? "CC001",
                    ["JobTitle"] = firstResult.JobTitle ?? "Manager",
                    ["RequesterName"] = firstResult.RequesterFullName ?? firstResult.REQ_BY ?? "Test User",
                    ["Nickname"] = firstResult.Nickname ?? "N/A",
                    ["EmployeeId"] = firstResult.EmployeeNo ?? firstResult.EMP_NO ?? "EMP001",
                    ["MobilePhone"] = firstResult.MobilePhone ?? "N/A",
                    ["NameSurname"] = firstResult.NameSurname ?? firstResult.REQUESTER_NAME_ENG ?? "Mr. Test User",
                    ["CoordinatorName"] = firstResult.CoordinatorFullName ?? firstResult.COORDINATOR_NAME ?? "Coordinator Name",
                    ["CoordinatorPhone"] = firstResult.CoordinatorPhone ?? "082-345-6789",
                    ["ApplicantID"] = firstResult.ApplicantID ?? 0,
                    ["StartDate"] = firstResult.StartDate is DateTime startDate ? startDate : DateTime.Now,
                    ["ITRequests"] = itRequests,
                    ["ServicesList"] = servicesList,
                    ["SignaturesList"] = signaturesList,
                    ["NewUserDetails"] = firstResult.REQ_DETAIL ?? "Request user for new employee: New User",
                    ["ServiceDetails"] = firstResult.REQUEST_DETAILS ?? "N/A",
                    ["ReceivedDate"] = firstResult.IT_ACK_DATE is DateTime itDate ? itDate : DateTime.Now,
                    ["AssignedTo"] = firstResult.IT_PIC ?? "IT Support",
                    ["ITDetails"] = firstResult.IT_COMMENT ?? "Installation completed",
                    ["Priority"] = firstResult.REQ_LEVEL ?? "Medium",
                    ["RequesterDate"] = firstResult.RequesterTimestamp is DateTime reqTimestamp ? reqTimestamp : (firstResult.REQ_DATE is DateTime reqDate ? reqDate : DateTime.Now),
                    ["ApproverText"] = firstResult.APPROVE_BY ?? "Approver Manager",
                    ["ApproverDate"] = firstResult.ApproverTimestamp is DateTime appTimestamp ? appTimestamp : (firstResult.REQ_DATE is DateTime appDate ? appDate : DateTime.Now),
                    ["UatUser"] = firstResult.ACK_BY ?? "Test User",
                    ["UatDate"] = firstResult.UATUserTimestamp is DateTime uatTimestamp ? uatTimestamp : (firstResult.REQ_DATE is DateTime uatDate ? uatDate : DateTime.Now),
                    ["ITOfficer"] = firstResult.CLOSE_BY ?? "IT Officer",
                    ["ITDate"] = firstResult.ITOfficerTimestamp is DateTime itOfficerTimestamp ? itOfficerTimestamp : (firstResult.REQ_DATE is DateTime itOfficerDate ? itOfficerDate : DateTime.Now),
                    ["OtherApproverText"] = firstSignature != null && firstSignature.TryGetValue("OtherApproverSignature", out var otherAppSig) && otherAppSig != null ? "Other Approver" : "N/A",
                    ["OtherApproverDate"] = firstSignature != null && firstSignature.TryGetValue("OtherApproverTimestamp", out var otherAppTs) && otherAppTs is DateTime otherAppTimestamp ? otherAppTimestamp : DateTime.Now,
                    ["UatUser2"] = firstSignature != null && firstSignature.TryGetValue("OtherUATUserSignature", out var otherUatSig) && otherUatSig != null ? "Other UAT User" : "N/A",
                    ["UatDate2"] = firstSignature != null && firstSignature.TryGetValue("OtherUATUserTimestamp", out var otherUatTs) && otherUatTs is DateTime otherUatTimestamp ? otherUatTimestamp : DateTime.Now
                };

                var viewAsPdf = new ViewAsPdf("ITRequestForm", dataDict)
                {
                    FileName = $"ITRequest_{(reqNo ?? "Applicant_" + applicantId ?? "All")}.pdf",
                    PageSize = Rotativa.AspNetCore.Options.Size.A4,
                    PageMargins = new Rotativa.AspNetCore.Options.Margins(10, 10, 20, 10),
                    CustomSwitches = "--encoding UTF-8 --disable-smart-shrinking --dpi 300"
                };

                return viewAsPdf;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in GenerateForm: {Message}", ex.Message);
                return StatusCode(500, new { Error = $"Error generating PDF: {ex.Message}" });
            }
        }

        private async Task SendITRequestEmail(List<Dictionary<string, object>> requestDataList, int id, bool isUpdate, string? reqNo, string? approver1, string? approver2, string? approver3, string? approver4, string? approver5)
        {
            try
            {
                string requesterEmail = _defaultEmail;
                if (requestDataList.Any(data => data.TryGetValue("RequesterEmail", out var emailObj) && emailObj is string email && IsValidEmail(email)))
                {
                    requesterEmail = requestDataList.First(data => data.TryGetValue("RequesterEmail", out var emailObj) && emailObj is string email && IsValidEmail(email))["RequesterEmail"].ToString()!;
                }

                string action = isUpdate ? "updated" : "created";
                reqNo ??= requestDataList.FirstOrDefault()?.TryGetValue("REQ_NO", out var reqNoObj) == true && reqNoObj != null ? reqNoObj.ToString()! : "N/A";
                string subject = $"IT Request #{reqNo} has been {action}";

                // Build email body with all services
                var servicesList = new System.Text.StringBuilder();
                foreach (var requestData in requestDataList)
                {
                    servicesList.AppendLine("<div style='margin-bottom: 15px;'>");
                    servicesList.AppendLine($"<p><strong>Service:</strong> {(requestData.TryGetValue("SERVICE_ID", out var serviceId) && serviceId != null ? serviceId.ToString() : "N/A")}</p>");
                    servicesList.AppendLine($"<p><strong>Details:</strong> {(requestData.TryGetValue("REQ_DETAIL", out var detail) && detail != null ? detail.ToString() : "N/A")}</p>");
                    servicesList.AppendLine($"<p><strong>Request Details:</strong> {(requestData.TryGetValue("REQUEST_DETAILS", out var reqDetails) && reqDetails != null ? reqDetails.ToString() : "N/A")}</p>");
                    servicesList.AppendLine($"<p><strong>Status:</strong> {(requestData.TryGetValue("REQ_STATUS", out var statusObj) && statusObj != null ? statusObj.ToString() : "N/A")}</p>");
                    servicesList.AppendLine($"<p><strong>File:</strong> {(requestData.TryGetValue("FilePath", out var filePath) && filePath != null ? $"<a href='{System.Web.HttpUtility.HtmlEncode(filePath)}'>View File</a>" : "N/A")}</p>");
                    servicesList.AppendLine("</div>");
                }

                // Email for Approvers
                var approvers = new[] { approver1, approver2, approver3, approver4, approver5 }
                    .Where(a => !string.IsNullOrWhiteSpace(a) && IsValidEmail(a))
                    .ToList();

                //if (approvers.Count != 0)
                //{
                //    string approverBody = $"""
                //    <div style='font-family: Arial, sans-serif; padding: 20px;'>
                //        <p>An IT request #{reqNo} requires your approval.</p>
                //        <h3>Services Requested:</h3>
                //        {servicesList}
                //        <p>View details at <a href='https://your-app.com/it-requests/{id}'>Request #{reqNo}</a>.</p>
                //    </div>
                //    """;

                //    foreach (var approver in approvers)
                //    {
                //        if (approver != null)
                //        {
                //            await _emailService.SendEmailAsync(approver, subject, approverBody, true, "IT-Request", null);
                //        }
                //    }
                //}

                //// Email for Requester
                //if (IsValidEmail(requesterEmail))
                //{
                //    string requesterBody = $"""
                //    <div style='font-family: Arial, sans-serif; padding: 20px;'>
                //        <p>Your IT request #{reqNo} has been {action}.</p>
                //        <h3>Services Requested:</h3>
                //        {servicesList}
                //        <p>View details at <a href='https://your-app.com/it-requests/{id}'>Request #{reqNo}</a>.</p>
                //    </div>
                //    """;

                //    await _emailService.SendEmailAsync(requesterEmail, subject, requesterBody, true, "IT-Request", null);
                //}
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email for IT request #{id}: {Message}", id, ex.Message);
            }
        }

        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private static async Task<string?> ConvertFileToBase64(IFormFile? file)
        {
            if (file == null || file.Length == 0)
                return null;

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            var base64String = $"data:{file.ContentType};base64,{Convert.ToBase64String(memoryStream.ToArray())}";
            return base64String;
        }
        [HttpGet("dataUserAdmin")]
        [TypeFilter(typeof(JwtAuthorizeAttribute))]
        public async Task<IActionResult> GetDataUserAdmin([FromQuery] int? JobID)
        {
            try
            {
                using var connection = new SqlConnection(_dbConnection.ConnectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@JobID", JobID);
                // parameters.Add("@ApplicantID", ApplicantID);
                // sp_listNameSignatures
                var result = await connection.QueryAsync(new CommandDefinition(
                    "sp_listNameSignaturesV2",
                    parameters,
                    commandType: CommandType.StoredProcedure));

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetDataUserAdmin");
                return StatusCode(500, new { Error = "Internal server error", Details = ex.Message });
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
                    await _emailService.SendEmailAsync(email, subject, body, true, "IT-Request", null);
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

        [HttpGet("dataUserAdminIT")]
        [TypeFilter(typeof(JwtAuthorizeAttribute))]
        public async Task<IActionResult> GetDataUserAdminIT()
        {
            try
            {
                using var connection = new SqlConnection(_dbConnection.ConnectionString);
                var parameters = new DynamicParameters();
                var result = await connection.QueryAsync(new CommandDefinition(
                    "sp_listNameSignaturesIT",
                    commandType: CommandType.StoredProcedure));

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetDataUserAdmin");
                return StatusCode(500, new { Error = "Internal server error", Details = ex.Message });
            }
        }
        private async Task<int> SendEmailsITAsync(int ApplicantID, string? TypeCondition, string? Name, string? CostCenter)
        {
            using var connection = new SqlConnection(_dbConnection.ConnectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@ApplicantID", ApplicantID);
            parameters.Add("@TypeCondition", TypeCondition);

            var result = await connection.QueryAsync<dynamic>(
                "EXEC sp_GetDateSendEmailITV2 @ApplicantID, @TypeCondition",
                parameters);
            var emails = result
                .Select(r => ((string?)r?.EMAIL)?.Trim())
                .Where(email => !string.IsNullOrWhiteSpace(email))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var firstRecord = result.FirstOrDefault();
            string hrBody = string.Empty;
            string SubjectMail = string.Empty;
            if (TypeCondition == "REQIT")
            {
                hrBody = $@"
                    <div style='font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px; font-size: 14px; line-height: 1.6;'>
                        <p style='margin: 0; font-weight: bold;'>ขอความกรุณาอนุมัติคำขอใช้งาน IT - คุณ {firstRecord?.FirstNameThai} {firstRecord?.LastNameThai}</p>
                        <p style='margin: 0;'>เรียน คุณ{firstRecord?.NAMFIRSTT} {firstRecord?.NAMLASTT}</p>
                        <p>ขอแจ้งให้ทราบว่า มีคำขอใช้งานระบบ IT สำหรับ คุณ {firstRecord?.FirstNameThai} {firstRecord?.LastNameThai} เข้ามา <br>
                            กรุณาพิจารณาและดำเนินการอนุมัติผ่านระบบตามความเหมาะสม
                        </p>
                        <p style='margin: 0;'>กรุณาคลิก Link:
                            <a target='_blank' href='https://oneejobs27.oneeclick.co:7191/LoginAdmin?ApId={ApplicantID}&ITReq=FromMailIT'
                                style='color: #007bff; text-decoration: underline;'>
                                https://oneejobs27.oneeclick.co
                            </a>
                            เพื่อพิจารณาอนุมัติคำขอ
                        </p>     
                        <p style='margin-top: 30px; margin:0'>ด้วยความเคารพ,</p>
                        <p style='margin: 0;'>{Name}</p>
                        <p style='margin: 0;'>แผนก {CostCenter}</p>
                        <br>
                        <p style='color:red; font-weight: bold;'>**อีเมลนี้คือข้อความอัตโนมัติ กรุณาอย่าตอบกลับ**</p>
                    </div>";
                SubjectMail = $@"ขอความกรุณาอนุมัติคำขอใช้งาน IT - คุณ {firstRecord?.FirstNameThai} {firstRecord?.LastNameThai}";
            }
            if (TypeCondition == "RESIT")
            {
                hrBody = $@"
                    <div style='font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px; font-size: 14px; line-height: 1.6;'>
                        <p style='margin: 0; font-weight: bold;'>แจ้งคำขอใช้งาน IT ได้รับอนุมัติ - คุณ {firstRecord?.FirstNameThai} {firstRecord?.LastNameThai}</p>
                        <p style='margin: 0;'>เรียนทีม IT,</p>
                        <p>คำขอใช้งานระบบสำหรับ คุณ {firstRecord?.FirstNameThai} {firstRecord?.LastNameThai} ได้รับการอนุมัติเรียบร้อยแล้ว 
                            <br>ขอความกรุณาดำเนินการจัดเตรียมตามขั้นตอนที่เกี่ยวข้องต่อไปค่ะ
                        </p>
                        <p style='margin-top: 30px; margin:0'>ด้วยความเคารพ,</p>
                        <p style='margin: 0;'>{firstRecord?.ApproveNameThai}</p>
                        <br>
                        <p style='color:red; font-weight: bold;'>**อีเมลนี้คือข้อความอัตโนมัติ กรุณาอย่าตอบกลับ**</p>
                    </div>";
                SubjectMail = $@"คำขอใช้งาน IT - คุณ {firstRecord?.FirstNameThai} {firstRecord?.LastNameThai}";
            }
            if (TypeCondition == "ITCompleted")
            {
                hrBody = $@"
                    <div style='font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px; font-size: 14px; line-height: 1.6;'>
                        <p style='margin: 0; font-weight: bold;'>แจ้งผลการดำเนินการ IT - คุณ {firstRecord?.FirstNameThai} {firstRecord?.LastNameThai}</p>
                        <p style='margin: 0;'>เรียนคุณ ผู้เกี่ยวข้องทุกท่าน</p>
                        <p>ทีม IT ได้ดำเนินการตามคำขอสำหรับคุณ {firstRecord?.FirstNameThai} {firstRecord?.LastNameThai} เรียบร้อยแล้วค่ะ <br>
                            หากมีข้อสอบถามเพิ่มเติม หรือต้องการความช่วยเหลืออื่นใด สามารถแจ้งปัญหา itsupport@onee.one</p>
                        <p style='margin-top: 30px; margin:0'>ด้วยความเคารพ,</p>
                        <p style='margin: 0;'>IT Department</p>
                        <br>
                        <p style='color:red; font-weight: bold;'>**อีเมลนี้คือข้อความอัตโนมัติ กรุณาอย่าตอบกลับ**</p>
                    </div>";
                SubjectMail = $@"แจ้งผลการดำเนินการ IT - คุณ {firstRecord?.FirstNameThai} {firstRecord?.LastNameThai}";
            }
            if (TypeCondition == "ITDIRECTOR")
            {
                hrBody = $@"
                    <div style='font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px; font-size: 14px; line-height: 1.6;'>
                        <p style='margin: 0;'>เรียน คุณ{firstRecord?.NAMFIRSTT} {firstRecord?.NAMLASTT}</p>
                        <p>ขอแจ้งให้ทราบว่า มีคำขอใช้งานระบบ IT สำหรับ คุณ{firstRecord?.FirstNameThai} {firstRecord?.LastNameThai} เข้ามา <br>
                            กรุณาพิจารณาและดำเนินการอนุมัติผ่านระบบตามความเหมาะสม
                        </p>
                        <p style='margin: 0;'>กรุณาคลิก Link:
                            <a target='_blank' href='https://oneejobs27.oneeclick.co:7191/LoginAdmin?ApId={ApplicantID}&ITReq=ITDIRECTOR'
                                style='color: #007bff; text-decoration: underline;'>
                                https://oneejobs27.oneeclick.co
                            </a>
                            เพื่อพิจารณาอนุมัติคำขอ
                        </p>  
                        <p style='margin-top: 30px; margin:0'>ด้วยความเคารพ,</p>
                        <br>
                        <p style='margin: 0;'>ผู้ขอ:{Name}</p>
                        <p style='margin: 0;'>หน่วยงาน: {CostCenter}</p>
                        <p style='margin: 0;'>เบอร์โทร: </p>
                        <p style='margin: 0;'>Email: </p>
                        <br>
                        <p style='color:red; font-weight: bold;'>**อีเมลนี้คือข้อความอัตโนมัติ กรุณาอย่าตอบกลับ**</p>
                    </div>";
                SubjectMail = $@"ขอความกรุณาอนุมัติคำขอใช้งาน  IT - คุณ {firstRecord?.FirstNameThai} {firstRecord?.LastNameThai}";
            }
            if (TypeCondition == "ITACKNOWLEDGE")
            {
                hrBody = $@"
                    <div style='font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px; font-size: 14px; line-height: 1.6;'>
                        <p style='margin: 0;'>เรียนคุณ ผู้เกี่ยวข้องทุกท่าน</p>
                        <p style='padding-left: 20px;'>ทาง IT ได้รับเรื่องคำขอของท่านแล้ว และดำเนินการตามคำขอของท่าน โดยจะทำการอัพเดตความคืบหน้าผ่านระบบ<br>
                            โดยท่านจะได้รับ Email แจ้งเตือนอีกครั้งเมื่อมีความคืบหน้า</p>
                        <p style='margin: 0;'>ติดตามความคืบหน้าของคำขอของท่านผ่านลิงค์ Link:
                            <a target='_blank' href='https://oneejobs27.oneeclick.co:7191/LoginAdmin?ApId={ApplicantID}&ITReq=ITACKNOWLEDGE'
                                style='color: #007bff; text-decoration: underline;'>
                                https://oneejobs27.oneeclick.co
                            </a>
                            เพื่อตรวจสอบความคืบหน้าของคำขอ
                        </p>
                        <p style='margin-top: 30px; margin:0'>ด้วยความเคารพ,</p>
                        <p style='margin: 0;'>IT Department</p>
                        <br>
                        <p style='color:red; font-weight: bold;'>**อีเมลนี้คือข้อความอัตโนมัติ กรุณาอย่าตอบกลับ**</p>
                    </div>";
                SubjectMail = $@"ความคืบหน้าของสถานะคำขอใช้งานของ คุณ {firstRecord?.FirstNameThai} {firstRecord?.LastNameThai}";
            }

            return await SendEmailsAsync(emails!, SubjectMail, hrBody);
        }

    }
}