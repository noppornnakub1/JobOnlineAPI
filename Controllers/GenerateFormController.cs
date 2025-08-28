using JobOnlineAPI.Views.Register;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System.Data;
using System.Data.SqlClient;
using Dapper;
using System.Text.Json;

namespace JobOnlineAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GenerateFormController(IWebHostEnvironment env, IConfiguration config) : ControllerBase
    {
        private readonly IWebHostEnvironment _env = env ?? throw new ArgumentNullException(nameof(env));
        private readonly IConfiguration _config = config;

        [HttpPost("GenerateRegisterFormPDF")]
        public IActionResult GenerateRegisterFormPDF([FromBody] JsonElement request)
        {
            int applicantId = request.GetProperty("ApplicantID").GetInt32();
            int jobId = request.GetProperty("JobID").GetInt32();

            using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            var form = connection.QueryFirstOrDefault<dynamic>(
                "sp_GetDataGenRegisFormPDF",
                new { ApplicantID = applicantId, JobID = jobId },
                commandType: CommandType.StoredProcedure);

            if (form == null)
                return NotFound("ไม่พบข้อมูลผู้สมัคร");

            var dict = (IDictionary<string, object>)form;

            QuestPDF.Settings.License = LicenseType.Community;
            var pdf = new PersonalDetailsForm(form).GeneratePdf();

            return File(pdf, "application/pdf", $"form_{applicantId}.pdf");
        }
    }
}