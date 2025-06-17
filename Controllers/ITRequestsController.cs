using System.Text.Json;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using JobOnlineAPI.Services;

namespace JobOnlineAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ITRequestsController(IConfiguration configuration, IEmailService emailService) : ControllerBase
    {
        private readonly IDbConnection _dbConnection = new SqlConnection(configuration.GetConnectionString("DefaultConnection"));
        private readonly IEmailService _emailService = emailService;
        private readonly string _defaultEmail = "default@company.com";
        private readonly string _itTeamEmail = "it-team@company.com";

        [HttpPost]
        public async Task<IActionResult> SubmitITRequest([FromBody] JsonElement request)
        {
            try
            {
                // รองรับทั้ง JSON object และ JSON array
                if (request.ValueKind != JsonValueKind.Object && request.ValueKind != JsonValueKind.Array)
                    return BadRequest(new { Error = "Invalid JSON format. Expected a JSON object or array." });

                List<Dictionary<string, object>> requestDataList = [];
                string? createdBy = null;
                bool isArray = request.ValueKind == JsonValueKind.Array;

                // กรณี JSON object เดียว
                if (!isArray)
                {
                    if (!request.TryGetProperty("CreatedBy", out var createdByElement) || createdByElement.ValueKind == JsonValueKind.Null || string.IsNullOrWhiteSpace(createdByElement.GetString()))
                        return BadRequest(new { Error = "CreatedBy is required and cannot be empty." });

                    createdBy = createdByElement.GetString()!;
                    var requestData = new Dictionary<string, object>();
                    foreach (var property in request.EnumerateObject())
                    {
                        if (property.Name.Equals("REQ_NO", StringComparison.OrdinalIgnoreCase))
                            continue;

                        requestData[property.Name] = property.Value.ValueKind switch
                        {
                            JsonValueKind.String => property.Value.GetString() ?? string.Empty,
                            JsonValueKind.Number => property.Value.TryGetInt32(out int intValue) ? intValue : property.Value.GetDouble(),
                            JsonValueKind.True => true,
                            JsonValueKind.False => false,
                            JsonValueKind.Null => string.Empty,
                            JsonValueKind.Undefined => throw new NotImplementedException("Undefined JSON value not supported"),
                            JsonValueKind.Object => throw new NotImplementedException("Object JSON value not supported"),
                            JsonValueKind.Array => throw new NotImplementedException("Array JSON value not supported"),
                            _ => property.Value.ToString() ?? string.Empty
                        };
                    }
                    requestDataList.Add(requestData);
                }
                // กรณี JSON array
                else
                {
                    foreach (var item in request.EnumerateArray())
                    {
                        if (item.ValueKind != JsonValueKind.Object)
                            return BadRequest(new { Error = "Each item in the array must be a JSON object." });

                        if (!item.TryGetProperty("CreatedBy", out var createdByElement) || createdByElement.ValueKind == JsonValueKind.Null || string.IsNullOrWhiteSpace(createdByElement.GetString()))
                            return BadRequest(new { Error = "CreatedBy is required and cannot be empty in each array item." });

                        if (createdBy == null)
                            createdBy = createdByElement.GetString()!;
                        else if (createdBy != createdByElement.GetString())
                            return BadRequest(new { Error = "All items in the array must have the same CreatedBy value." });

                        var requestData = new Dictionary<string, object>();
                        foreach (var property in item.EnumerateObject())
                        {
                            if (property.Name.Equals("REQ_NO", StringComparison.OrdinalIgnoreCase))
                                continue;

                            requestData[property.Name] = property.Value.ValueKind switch
                            {
                                JsonValueKind.String => property.Value.GetString() ?? string.Empty,
                                JsonValueKind.Number => property.Value.TryGetInt32(out int intValue) ? intValue : property.Value.GetDouble(),
                                JsonValueKind.True => true,
                                JsonValueKind.False => false,
                                JsonValueKind.Null => string.Empty,
                                JsonValueKind.Undefined => throw new NotImplementedException("Undefined JSON value not supported"),
                                JsonValueKind.Object => throw new NotImplementedException("Object JSON value not supported"),
                                JsonValueKind.Array => throw new NotImplementedException("Array JSON value not supported"),
                                _ => property.Value.ToString() ?? string.Empty
                            };
                        }
                        requestDataList.Add(requestData);
                    }
                }

                int id = 0;
                bool isUpdate = !isArray && request.TryGetProperty("ID", out var idElement) && idElement.ValueKind != JsonValueKind.Null && idElement.TryGetInt32(out id);
                string operation = isUpdate ? "UPDATE" : "INSERT";

                // ตรวจสอบ REQ_STATUS สำหรับ INSERT
                foreach (var requestData in requestDataList)
                {
                    if (requestData.TryGetValue("REQ_STATUS", out var statusObj) && statusObj is string reqStatus && !isUpdate && reqStatus is "Acknowledge" or "Completed")
                        return BadRequest(new { Error = "INSERT operation requires REQ_STATUS to be 'New'." });
                }

                try
                {
                    using var connection = new SqlConnection(_dbConnection.ConnectionString);
                    var parameters = new DynamicParameters();
                    parameters.Add("Operation", operation);
                    parameters.Add("ID", isUpdate ? id : (object?)null, DbType.Int32);
                    string jsonData = isArray ? JsonSerializer.Serialize<List<Dictionary<string, object>>>(requestDataList) : JsonSerializer.Serialize<Dictionary<string, object>>(requestDataList[0]);
                    parameters.Add("JsonData", jsonData);
                    parameters.Add("CreatedBy", createdBy);
                    parameters.Add("NewID", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("ErrorMessage", dbType: DbType.String, direction: ParameterDirection.Output, size: 500);

                    var result = await connection.QueryAsync(
                        "usp_DynamicInsertUpdateT_EMP_IT_REQ",
                        parameters,
                        commandType: CommandType.StoredProcedure
                    );

                    var newId = parameters.Get<int?>("NewID");
                    var errorMessage = parameters.Get<string>("ErrorMessage");

                    if (!string.IsNullOrEmpty(errorMessage))
                        return BadRequest(new { Error = errorMessage });

                    if (newId == null)
                        return StatusCode(500, new { Error = $"Failed to {(isUpdate ? "update" : "create")} IT request." });

                    // สร้าง response จาก result set
                    int index = 0;
                    var responseItems = result.Select(r =>
                    {
                        // ใช้ requestData ตาม index สำหรับ array
                        var requestData = isArray && index < requestDataList.Count ? requestDataList[index++] : requestDataList.FirstOrDefault();
                        return new
                        {
                            ITRequestId = r.NewID,
                            r.ReqNo,
                            Message = $"IT request {(isUpdate ? "updated" : "created")} successfully.",
                            FilePath = requestData != null && requestData.TryGetValue("FilePath", out var filePath) && filePath != null ? filePath.ToString() : null
                        };
                    }).ToList();

                    // ส่งอีเมลแจ้งเตือนสำหรับแต่ละ result
                    index = 0;
                    foreach (var r in result)
                    {
                        string? reqNo = r.ReqNo;
                        string? approver1 = r.APPROVER1;
                        string? approver2 = r.APPROVER2;
                        string? approver3 = r.APPROVER3;
                        string? approver4 = r.APPROVER4;
                        string? approver5 = r.APPROVER5;

                        // ใช้ requestData ตาม index สำหรับ array
                        var requestData = isArray && index < requestDataList.Count ? requestDataList[index] : requestDataList.FirstOrDefault() ?? [];
                        await SendITRequestEmail(requestData, (int)r.NewID, isUpdate, reqNo, approver1, approver2, approver3, approver4, approver5);
                        index++;
                    }

                    return Ok(new
                    {
                        ITRequests = responseItems,
                        Message = isUpdate ? "IT request updated successfully." : "Multiple IT requests created successfully."
                    });
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { Error = "Internal Server Error", Details = ex.Message });
                }
            }
            catch (JsonException ex)
            {
                return BadRequest(new { Error = $"Invalid JSON format: {ex.Message}" });
            }
        }

        private async Task SendITRequestEmail(Dictionary<string, object> requestData, int id, bool isUpdate, string? reqNo, string? approver1, string? approver2, string? approver3, string? approver4, string? approver5)
        {
            try
            {
                string requesterEmail = _defaultEmail;
                if (requestData.TryGetValue("RequesterEmail", out var emailObj) && emailObj is string email && IsValidEmail(email))
                {
                    requesterEmail = email;
                }

                string action = isUpdate ? "updated" : "created";
                reqNo ??= requestData.TryGetValue("REQ_NO", out var reqNoObj) && reqNoObj != null ? reqNoObj.ToString()! : "N/A";
                string reqStatus = requestData.TryGetValue("REQ_STATUS", out var statusObj) && statusObj != null ? statusObj.ToString()! : "N/A";
                string subject = $"IT Request #{reqNo} has been {action}";

                // อีเมลสำหรับ Approvers
                var approvers = new[] { approver1, approver2, approver3, approver4, approver5 }
                    .Where(a => !string.IsNullOrWhiteSpace(a) && IsValidEmail(a))
                    .ToList();

                if (approvers.Count != 0)
                {
                    string approverBody = $"""
                <div style='font-family: Arial, sans-serif; padding: 20px;'>
                    <p>An IT request #{reqNo} requires your approval.</p>
                    <p><strong>Service:</strong> {(requestData.TryGetValue("SERVICE_ID", out var serviceId) && serviceId != null ? serviceId.ToString() : "N/A")}</p>
                    <p><strong>Details:</strong> {(requestData.TryGetValue("REQ_DETAIL", out var detail) && detail != null ? detail.ToString() : "N/A")}</p>
                    <p><strong>Status:</strong> {reqStatus}</p>
                    <p><strong>File:</strong> {(requestData.TryGetValue("FilePath", out var filePath) && filePath != null ? $"<a href='{System.Web.HttpUtility.HtmlEncode(filePath)}'>View File</a>" : "N/A")}</p>
                    <p>View details at <a href='https://your-app.com/it-requests/{id}'>Request #{reqNo}</a>.</p>
                </div>
                """;

                    foreach (var approver in approvers)
                    {
                        if (approver != null)
                        {
                            await _emailService.SendEmailAsync(approver, $"Approval Required: IT Request #{reqNo}", approverBody, true);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send email for IT request #{id}: {ex.Message}");
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
    }
}