using JobOnlineAPI.Views.Register;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System.Data;
using System.Data.SqlClient;
using Dapper;
using System.Text.Json;
using JobOnlineAPI.Filters;


namespace JobOnlineAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GenerateFormController(IWebHostEnvironment env, IConfiguration config) : ControllerBase
    {
        private readonly IWebHostEnvironment _env = env ?? throw new ArgumentNullException(nameof(env));
        private readonly IConfiguration _config = config;

        // [HttpPost("GenerateRegisterFormPDF")]
        // [TypeFilter(typeof(JwtAuthorizeAttribute))]
        // public IActionResult GenerateRegisterFormPDF([FromBody] JsonElement request)
        // {
        //     int applicantId = request.GetProperty("ApplicantID").GetInt32();
        //     int jobId = request.GetProperty("JobID").GetInt32();

        //     using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
        //     var form = connection.QueryFirstOrDefault<dynamic>(
        //         "sp_GetDataGenRegisFormPDF",
        //         new { ApplicantID = applicantId, JobID = jobId },
        //         commandType: CommandType.StoredProcedure);

        //     if (form == null)
        //         return NotFound("ไม่พบข้อมูลผู้สมัคร");

        //     var dict = (IDictionary<string, object>)form;

        //     QuestPDF.Settings.License = LicenseType.Community;
        //     var pdf = new PersonalDetailsForm(form).GeneratePdf();

        //     return File(pdf, "application/pdf", $"form_{applicantId}.pdf");
        // }
        [HttpPost("GenerateRegisterFormPDF")]
        [TypeFilter(typeof(JwtAuthorizeAttribute))]
        public IActionResult GenerateRegisterFormPDF([FromBody] JsonElement request)
        {
            try
            {
                if (!request.TryGetProperty("ApplicantID", out var applicantIdEl) ||
                    !request.TryGetProperty("JobID", out var jobIdEl))
                {
                    return BadRequest("ApplicantID หรือ JobID ไม่ถูกต้อง");
                }

                int applicantId = applicantIdEl.GetInt32();
                int jobId = jobIdEl.GetInt32();

                using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
                var form = connection.QueryFirstOrDefault<dynamic>(
                    "sp_GetDataGenRegisFormPDF",
                    new { ApplicantID = applicantId, JobID = jobId },
                    commandType: CommandType.StoredProcedure);

                if (form == null)
                    return NotFound("ไม่พบข้อมูลผู้สมัคร");

                var dict = form as IDictionary<string, object>;
                if (dict == null)
                    return StatusCode(500, "ไม่สามารถแปลงข้อมูลผู้สมัครได้");

                QuestPDF.Settings.License = LicenseType.Community;
                var pdf = new PersonalDetailsForm(dict).GeneratePdf();

                return File(pdf, "application/pdf", $"form_{applicantId}.pdf");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, stack = ex.StackTrace });
            }
        }

        [HttpPost("GenerateRegisterFormPDFV2")]
        // [TypeFilter(typeof(JwtAuthorizeAttribute))]
        public IActionResult GenerateRegisterFormPDFV2([FromBody] JsonElement request)
        {
            try
            {
                if (!request.TryGetProperty("ApplicantID", out var applicantIdEl) ||
                    !request.TryGetProperty("JobID", out var jobIdEl))
                {
                    return BadRequest("ApplicantID หรือ JobID ไม่ถูกต้อง");
                }

                int applicantId = applicantIdEl.GetInt32();
                int jobId = jobIdEl.GetInt32();

                using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
                var form = connection.QueryFirstOrDefault<dynamic>(
                    "sp_GetDataGenRegisFormPDF",
                    new { ApplicantID = applicantId, JobID = jobId },
                    commandType: CommandType.StoredProcedure);

                if (form == null)
                    return NotFound("ไม่พบข้อมูลผู้สมัคร");

                var dict = form as IDictionary<string, object>;
                if (dict == null)
                    return StatusCode(500, "ไม่สามารถแปลงข้อมูลผู้สมัครได้");

                QuestPDF.Settings.License = LicenseType.Community;
                var pdf = new PersonalDetailsV2Form(dict).GeneratePdf();

                return File(pdf, "application/pdf", $"form_{applicantId}.pdf");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, stack = ex.StackTrace });
            }
        }

        [HttpPost("GenerateRegisterFormPDFV3")]
        // [TypeFilter(typeof(JwtAuthorizeAttribute))]
        public IActionResult GenerateRegisterFormPDFV3([FromBody] JsonElement request)
        {
            try
            {
                if (!request.TryGetProperty("ApplicantID", out var applicantIdEl) ||
                    !request.TryGetProperty("JobID", out var jobIdEl))
                {
                    return BadRequest("ApplicantID หรือ JobID ไม่ถูกต้อง");
                }

                int applicantId = applicantIdEl.GetInt32();
                int jobId = jobIdEl.GetInt32();

                using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
                var form = connection.QueryFirstOrDefault<dynamic>(
                    "sp_GetDataGenRegisFormPDF",
                    new { ApplicantID = applicantId, JobID = jobId },
                    commandType: CommandType.StoredProcedure);

                if (form == null)
                    return NotFound("ไม่พบข้อมูลผู้สมัคร");

                var dict = form as IDictionary<string, object>;
                if (dict == null)
                    return StatusCode(500, "ไม่สามารถแปลงข้อมูลผู้สมัครได้");

                QuestPDF.Settings.License = LicenseType.Community;
                var pdf = new PersonalDetailsV3Form(dict).GeneratePdf();

                return File(pdf, "application/pdf", $"form_{applicantId}.pdf");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, stack = ex.StackTrace });
            }
        }
    }
}