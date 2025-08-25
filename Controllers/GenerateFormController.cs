using JobOnlineAPI.Views.Register;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System.Data;
using System.Data.SqlClient;
using Dapper;



namespace JobOnlineAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GenerateFormController(IWebHostEnvironment env, IConfiguration config) : ControllerBase
    {
        private readonly IWebHostEnvironment _env = env ?? throw new ArgumentNullException(nameof(env));
        private readonly IConfiguration _config = config;

        [HttpPost("generate-form")]
        public IActionResult GenerateForm([FromBody] PersonalDetailsForm form)
        {
            // ตรวจสอบข้อมูลที่รับมา
            if (form == null || string.IsNullOrWhiteSpace(form.FullNameTH) || string.IsNullOrWhiteSpace(form.IDCard))
                return BadRequest("ข้อมูลที่จำเป็น (เช่น ชื่อ, บัตรประชาชน) หายไป");

            // ตรวจสอบรูปแบบวันที่
            if (!string.IsNullOrWhiteSpace(form.BirthDate) && !DateTime.TryParseExact(form.BirthDate, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out var birthDate))
                return BadRequest("รูปแบบวันเกิดไม่ถูกต้อง ต้องเป็น dd/MM/yyyy");

            // ตรวจสอบไฟล์ภาพ
            var imagePath = Path.Combine(_env.ContentRootPath, "Views", "imagesform", "one_logo.png");
            if (!System.IO.File.Exists(imagePath))
                return StatusCode(500, $"ไม่พบไฟล์ภาพ: {imagePath}");

            QuestPDF.Settings.License = LicenseType.Community;

            try
            {
                // สร้าง PDF ด้วย QuestPDF
                byte[] pdf = Document.Create(container => form.Compose(container)).GeneratePdf();
                var fileName = $"personal-details_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N")[..8]}.pdf";
                return File(pdf, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                // Log error (แนะนำให้ใช้ logging framework เช่น Serilog)
                return StatusCode(500, $"ข้อผิดพลาดในการสร้าง PDF: {ex.Message}");
            }
        }
        
        // [HttpPost("generate-form")]
        // public IActionResult GenerateForm([FromBody] dynamic request)
        // {
        //     int applicantId = request.ApplicantID;
        //     int jobId = request.JobID;

        //     using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
        //     var form = connection.QueryFirstOrDefault<dynamic>(
        //         "sp_GetApplicantDataV1",
        //         new { ApplicantID = applicantId, JobID = jobId },
        //         commandType: CommandType.StoredProcedure);

        //     if (form == null)
        //         return NotFound("ไม่พบข้อมูลผู้สมัคร");

        //     QuestPDF.Settings.License = LicenseType.Community;
        //     var pdf = new PersonalDetailsForm(form).GeneratePdf();  // 🔑 ส่ง dynamic เข้าไปเลย

        //     return File(pdf, "application/pdf", $"form_{applicantId}.pdf");
        // }
    }
}