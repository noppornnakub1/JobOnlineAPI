using JobOnlineAPI.Controllers;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace JobOnlineAPI.Filters
{
    public class ITRequestExampleOperationFilter(ILogger<ITRequestExampleOperationFilter> logger) : IOperationFilter
    {
        private readonly ILogger<ITRequestExampleOperationFilter> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            _logger.LogInformation("Applying filter. DeclaringType: {DeclaringType}, MethodName: {MethodName}, Assembly: {Assembly}",
                context.MethodInfo.DeclaringType?.FullName, context.MethodInfo.Name, context.MethodInfo.DeclaringType?.Assembly.FullName);

            if (context.MethodInfo.DeclaringType == typeof(ITRequestsController) && context.MethodInfo.Name == "SubmitITRequest")
            {
                _logger.LogInformation("Filter condition met for SubmitITRequest. Existing RequestBody: {RequestBody}",
                    operation.RequestBody != null ? "Exists" : "Null");

                operation.RequestBody = new OpenApiRequestBody
                {
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["multipart/form-data"] = new OpenApiMediaType
                        {
                            Schema = new OpenApiSchema
                            {
                                Type = "object",
                                Properties = new Dictionary<string, OpenApiSchema>
                                {
                                    ["requesterSignatureFile"] = new OpenApiSchema { Type = "file", Format = "binary", Nullable = true },
                                    ["approverSignatureFile"] = new OpenApiSchema { Type = "file", Format = "binary", Nullable = true },
                                    ["uatUserSignatureFile"] = new OpenApiSchema { Type = "file", Format = "binary", Nullable = true },
                                    ["itOfficerSignatureFile"] = new OpenApiSchema { Type = "file", Format = "binary", Nullable = true },
                                    ["otherApproverSignatureFile"] = new OpenApiSchema { Type = "file", Format = "binary", Nullable = true },
                                    ["otherUatUserSignatureFile"] = new OpenApiSchema { Type = "file", Format = "binary", Nullable = true },
                                    ["jsonData"] = new OpenApiSchema { Type = "string", Format = "json", Description = "JSON object or array of IT request details" },
                                    ["signatureId"] = new OpenApiSchema { Type = "string", Format = "int32", Description = "Signature ID for the request (optional)", Nullable = true }
                                },
                                Required = new HashSet<string> { "jsonData" }
                            }
                        }
                    }
                };

                if (!operation.RequestBody.Content["multipart/form-data"].Examples.ContainsKey("ITRequestExample"))
                {
                    operation.RequestBody.Content["multipart/form-data"].Examples.Add("ITRequestExample", new OpenApiExample
                    {
                        Value = new OpenApiObject
                        {
                            ["jsonData"] = new OpenApiString("""
                                [
                                    {
                                        "CreatedBy": "OTD01072",
                                        "REQ_NO": "250618-0001",
                                        "SERVICE_ID": 1,
                                        "REQ_DETAIL": "Test",
                                        "REQ_LEVEL": "Medium",
                                        "REQ_STATUS": "New",
                                        "RequesterEmail": "testuser@company.com"
                                    }
                                ]
                                """),
                            ["signatureId"] = new OpenApiString("29")
                        }
                    });
                }

                _logger.LogInformation("Filter applied successfully to SubmitITRequest, overriding default schema.");
            }
            else
            {
                _logger.LogWarning("Filter condition not met. DeclaringType: {DeclaringType}, MethodName: {MethodName}",
                    context.MethodInfo.DeclaringType?.FullName, context.MethodInfo.Name);
            }
        }
    }
}