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

        [HttpPost]
        public async Task<IActionResult> SubmitITRequest([FromBody] JsonElement request)
        {
            try
            {
                // Validate JSON format
                if (request.ValueKind != JsonValueKind.Object && request.ValueKind != JsonValueKind.Array)
                    return BadRequest(new { Error = "Invalid JSON format. Expected a JSON object or array." });

                List<Dictionary<string, object>> requestDataList = [];
                string? createdBy = null;
                bool isArray = request.ValueKind == JsonValueKind.Array;

                // Handle single JSON object
                if (!isArray)
                {
                    if (!request.TryGetProperty("CreatedBy", out var createdByElement) || createdByElement.ValueKind == JsonValueKind.Null || string.IsNullOrWhiteSpace(createdByElement.GetString()))
                        return BadRequest(new { Error = "CreatedBy is required and cannot be empty." });

                    createdBy = createdByElement.GetString()!;
                    var requestData = new Dictionary<string, object>();
                    foreach (var property in request.EnumerateObject())
                    {
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
                // Handle JSON array
                else
                {
                    string? jsonReqNo = null;
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
                            {
                                string? currentReqNo = property.Value.GetString();
                                if (currentReqNo != null)
                                {
                                    if (jsonReqNo == null)
                                        jsonReqNo = currentReqNo;
                                    else if (jsonReqNo != currentReqNo)
                                        return BadRequest(new { Error = "All items in the array must have the same REQ_NO value." });
                                }
                            }

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

                try
                {
                    using var connection = new SqlConnection(_dbConnection.ConnectionString);
                    var parameters = new DynamicParameters();
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
                        return StatusCode(500, new { Error = "Failed to process IT request." });

                    // Create response from result set
                    int index = 0;
                    var responseItems = result.Select(r =>
                    {
                        var requestData = isArray && index < requestDataList.Count ? requestDataList[index++] : requestDataList.FirstOrDefault();
                        return new
                        {
                            ITRequestId = r.NewID,
                            r.ReqNo,
                            Message = requestData != null && requestData.ContainsKey("ID") ? "IT request updated successfully." : "IT request created successfully.",
                            FilePath = requestData != null && requestData.TryGetValue("FilePath", out var filePath) && filePath != null ? filePath.ToString() : null
                        };
                    }).ToList();

                    // Send a single email for the entire request
                    var firstResult = result.FirstOrDefault();
                    if (firstResult != null)
                    {
                        string? reqNo = firstResult.ReqNo;
                        string? approver1 = firstResult.APPROVER1;
                        string? approver2 = firstResult.APPROVER2;
                        string? approver3 = firstResult.APPROVER3;
                        string? approver4 = firstResult.APPROVER4;
                        string? approver5 = firstResult.APPROVER5;
                        bool isUpdate = requestDataList.Any(data => data.ContainsKey("ID"));
                        await SendITRequestEmail(requestDataList, newId.Value, isUpdate, reqNo, approver1, approver2, approver3, approver4, approver5);
                    }

                    return Ok(new
                    {
                        ITRequests = responseItems,
                        Message = requestDataList.Any(data => data.ContainsKey("ID")) ? "IT requests updated successfully." : "Multiple IT requests created successfully."
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

        [HttpGet("{reqNo}")]
        public async Task<IActionResult> GetITRequestByReqNo(string reqNo)
        {
            try
            {
                using var connection = new SqlConnection(_dbConnection.ConnectionString);
                var parameters = new DynamicParameters();
                parameters.Add("REQ_NO", reqNo);
                parameters.Add("ErrorMessage", dbType: DbType.String, direction: ParameterDirection.Output, size: 500);

                var results = await connection.QueryAsync(
                    "usp_GetT_EMP_IT_REQ_ByReqNo",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var errorMessage = parameters.Get<string>("ErrorMessage");

                if (!string.IsNullOrEmpty(errorMessage))
                    return BadRequest(new { Error = errorMessage });

                if (results == null || !results.Any())
                    return Ok(new { ITRequests = new List<object>(), Message = "No IT requests found." });

                return Ok(new
                {
                    ITRequests = results,
                    Message = "IT requests retrieved successfully."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = "Internal Server Error", Details = ex.Message });
            }
        }

        [HttpGet("generate-form/{reqNo}")]
        public async Task<IActionResult> GenerateForm(string reqNo)
        {
            try
            {
                using var connection = new SqlConnection(_dbConnection.ConnectionString);
                var parameters = new DynamicParameters();
                parameters.Add("REQ_NO", reqNo);
                parameters.Add("ErrorMessage", dbType: DbType.String, direction: ParameterDirection.Output, size: 500);

                var results = await connection.QueryAsync<dynamic>(
                    "usp_GetT_EMP_IT_REQ_ByReqNo",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var errorMessage = parameters.Get<string>("ErrorMessage");

                if (!string.IsNullOrEmpty(errorMessage))
                    return BadRequest(new { Error = errorMessage });

                if (results == null || !results.Any())
                    return NotFound(new { Message = $"No IT requests found for REQ_NO: {reqNo}" });

                var dataDict = new Dictionary<string, object>
                {
                    ["Requests"] = results,
                    ["ReqNo"] = reqNo
                };

                var viewAsPdf = new Rotativa.AspNetCore.ViewAsPdf("ITRequestForm", dataDict)
                {
                    FileName = $"ITRequest_{reqNo}.pdf",
                    PageSize = Rotativa.AspNetCore.Options.Size.A4,
                    PageMargins = new Rotativa.AspNetCore.Options.Margins(10, 10, 20, 10)
                };
                return viewAsPdf;
            }
            catch (Exception ex)
            {
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
                    servicesList.AppendLine($"<p><strong>Status:</strong> {(requestData.TryGetValue("REQ_STATUS", out var statusObj) && statusObj != null ? statusObj.ToString() : "N/A")}</p>");
                    servicesList.AppendLine($"<p><strong>File:</strong> {(requestData.TryGetValue("FilePath", out var filePath) && filePath != null ? $"<a href='{System.Web.HttpUtility.HtmlEncode(filePath)}'>View File</a>" : "N/A")}</p>");
                    servicesList.AppendLine("</div>");
                }

                // Email for Approvers
                var approvers = new[] { approver1, approver2, approver3, approver4, approver5 }
                    .Where(a => !string.IsNullOrWhiteSpace(a) && IsValidEmail(a))
                    .ToList();

                if (approvers.Count != 0)
                {
                    string approverBody = $"""
                <div style='font-family: Arial, sans-serif; padding: 20px;'>
                    <p>An IT request #{reqNo} requires your approval.</p>
                    <h3>Services Requested:</h3>
                    {servicesList}
                    <p>View details at <a href='https://your-app.com/it-requests/{id}'>Request #{reqNo}</a>.</p>
                </div>
                """;

                    foreach (var approver in approvers)
                    {
                        if (approver != null)
                        {
                            await _emailService.SendEmailAsync(approver, subject, approverBody, true);
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