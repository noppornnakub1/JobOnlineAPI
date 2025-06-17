using System.Text.Json;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace JobOnlineAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ITRequestsController(IConfiguration configuration) : ControllerBase
    {
        private readonly IDbConnection _dbConnection = new SqlConnection(configuration.GetConnectionString("DefaultConnection"));

        [HttpPost]
        public async Task<IActionResult> SubmitITRequest([FromBody] JsonElement request)
        {
            try
            {
                if (request.ValueKind != JsonValueKind.Object)
                    return BadRequest(new { Error = "Invalid JSON format. Expected a JSON object." });
            }
            catch (JsonException ex)
            {
                return BadRequest(new { Error = $"Invalid JSON format: {ex.Message}" });
            }

            if (!request.TryGetProperty("CreatedBy", out var createdByElement) || createdByElement.ValueKind == JsonValueKind.Null || string.IsNullOrWhiteSpace(createdByElement.GetString()))
                return BadRequest(new { Error = "CreatedBy is required and cannot be empty." });

            string createdBy = createdByElement.GetString()!;

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

            int id = 0;
            bool isUpdate = request.TryGetProperty("ID", out var idElement) && idElement.ValueKind != JsonValueKind.Null && idElement.TryGetInt32(out id);
            string operation = isUpdate ? "UPDATE" : "INSERT";

            if (request.TryGetProperty("REQ_STATUS", out var statusElement) && statusElement.ValueKind != JsonValueKind.Null)
            {
                string? reqStatus = statusElement.GetString();
                if (!isUpdate && reqStatus is "Acknowledge" or "Completed")
                    return BadRequest(new { Error = "INSERT operation requires REQ_STATUS to be 'New'." });
            }

            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("Operation", operation);
                parameters.Add("ID", isUpdate ? id : (object?)null, DbType.Int32);
                parameters.Add("JsonData", JsonSerializer.Serialize(requestData));
                parameters.Add("CreatedBy", createdBy);
                parameters.Add("NewID", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("ErrorMessage", dbType: DbType.String, direction: ParameterDirection.Output, size: 500);

                var result = await _dbConnection.QueryFirstOrDefaultAsync<dynamic>(
                    "usp_DynamicInsertUpdateT_EMP_IT_REQ",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var newId = parameters.Get<int?>("NewID");
                var errorMessage = parameters.Get<string>("ErrorMessage");
                string? reqNo = result?.ReqNo;

                if (!string.IsNullOrEmpty(errorMessage))
                    return BadRequest(new { Error = errorMessage });

                if (newId == null)
                    return StatusCode(500, new { Error = $"Failed to {(isUpdate ? "update" : "create")} IT request." });

                return Ok(new
                {
                    ITRequestId = newId,
                    ReqNo = reqNo,
                    Message = $"IT request {(isUpdate ? "updated" : "created")} successfully.",
                    FilePath = requestData.TryGetValue("FilePath", out var filePath) && filePath != null ? filePath.ToString() : null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = "Internal Server Error", Details = ex.Message });
            }
        }

        /*
        private async Task SendITRequestEmail(Dictionary<string, object> requestData, int id, string createdBy, bool isUpdate)
        {
            // แก้ไขการกำหนด requesterEmail
            string requesterEmail = "default@company.com";
            if (requestData.TryGetValue("RequesterEmail", out var emailObj) && emailObj is string email)
            {
                requesterEmail = email;
            }

            string itTeamEmail = "it-team@company.com";
            string action = isUpdate ? "updated" : "created";
            string reqNo = requestData.TryGetValue("REQ_NO", out var reqNoObj) && reqNoObj != null ? reqNoObj.ToString()! : "N/A";
            string reqStatus = requestData.TryGetValue("REQ_STATUS", out var statusObj) && statusObj != null ? statusObj.ToString()! : "N/A";
            string subject = $"IT Request #{reqNo} has been {action}";

            // อีเมลสำหรับ requester
            string requesterBody = $"""
                <div style='font-family: Arial, sans-serif; padding: 20px;'>
                    <p>Your IT request #{reqNo} has been {action}.</p>
                    <p><strong>Service:</strong> {(requestData.TryGetValue("SERVICE_ID", out var serviceId) && serviceId != null ? serviceId.ToString() : "N/A")}</p>
                    <p><strong>Details:</strong> {(requestData.TryGetValue("REQ_DETAIL", out var detail) && detail != null ? detail.ToString() : "N/A")}</p>
                    <p><strong>Status:</strong> {reqStatus}</p>
                    <p><strong>File:</strong> {(requestData.TryGetValue("FilePath", out var filePath) && filePath != null ? $"<a href='{filePath}'>View File</a>" : "N/A")}</p>
                    <p>For more details, contact IT team at <a href='mailto:{itTeamEmail}'>{itTeamEmail}</a>.</p>
                </div>
                """;

            await _emailService.SendEmailAsync(requesterEmail, subject, requesterBody, true);

            // อีเมลสำหรับ IT team
            string itTeamBody = $"""
                <div style='font-family: Arial, sans-serif; padding: 20px;'>
                    <p>An IT request #{reqNo} has been {action} by {createdBy}.</p>
                    <p><strong>Service:</strong> {(requestData.TryGetValue("SERVICE_ID", out serviceId) && serviceId != null ? serviceId.ToString() : "N/A")}</p>
                    <p><strong>Details:</strong> {(requestData.TryGetValue("REQ_DETAIL", out detail) && detail != null ? detail.ToString() : "N/A")}</p>
                    <p><strong>Status:</strong> {reqStatus}</p>
                    <p><strong>File:</strong> {(requestData.TryGetValue("FilePath", out filePath) && filePath != null ? $"<a href='{filePath}'>View File</a>" : "N/A")}</p>
                    <p>View details at <a href='https://your-app.com/it-requests/{id}'>Request #{reqNo}</a>.</p>
                </div>
                """;

            await _emailService.SendEmailAsync(itTeamEmail, subject, itTeamBody, true);
        }
        */
    }
}