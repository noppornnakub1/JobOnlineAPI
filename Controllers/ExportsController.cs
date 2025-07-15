using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using System.Data;
using System.Data.SqlClient;
using Dapper;
using System.Text.Json;

namespace JobOnlineAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExportController(IConfiguration config) : ControllerBase
    {
        private readonly IConfiguration _config = config;

        [HttpPost("export-excel")]
        public async Task<IActionResult> ExportApplicantsAsync([FromForm] int? applicantId, [FromForm] int? userId)
        {
            if (applicantId == null && userId == null)
                return BadRequest("ต้องระบุอย่างน้อย applicantId หรือ userId");

            var connectionString = _config.GetConnectionString("DefaultConnection");

            using var connection = new SqlConnection(connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("ApplicantID", applicantId);
            parameters.Add("UserId", userId);

            var rawData = (await connection.QueryAsync(
                "sp_GetApplicantDataV2", parameters,
                commandType: CommandType.StoredProcedure)).ToList();

            var applicants = rawData
                .Select(row => (IDictionary<string, object>)row)
                .ToList();

            if (applicants.Count == 0)
                return NotFound("ไม่พบข้อมูลผู้สมัครที่ตรงกับเงื่อนไข");

            var excludedColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "EducationList", "WorkExperienceList", "SkillsList", "UserId", "FilesList"
            };

            var props = applicants[0].Keys
                .Where(k => !excludedColumns.Contains(k))
                .ToList();

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Applicants");

            var columnOrder = new List<(string Key, string Display)>
            {
                ("CodeMPID", "รหัสพนักงาน"),
                ("Title", "คำนำหน้า"),
                ("FirstNameThai", "ชื่อพนักงาน ภาษาไทย"),
                ("LastNameThai", "นามสกุล ภาษาไทย"),
                ("FirstNameEng", "ชื่อพนักงาน ภาษาอังกฤษ"),
                ("LastNameEng", "นามสกุล ภาษาอังกฤษ"),
                ("BirthDate", "วันเกิด"),
                ("MaritalStatus", "สถานภาพสมรส")
            };

            // Header
            for (int col = 0; col < columnOrder.Count; col++)
            {
                ws.Cells[1, col + 1].Value = columnOrder[col].Display;
                ws.Cells[1, col + 1].Style.Font.Bold = true;
            }

            // Data
            for (int row = 0; row < applicants.Count; row++)
            {
                var item = (IDictionary<string, object>)applicants[row];

                for (int col = 0; col < columnOrder.Count; col++)
                {
                    var key = columnOrder[col].Key;
                    ws.Cells[row + 2, col + 1].Value = item.TryGetValue(key, out object? value) ? value : null;
                }
            }

            ws.Cells.AutoFitColumns();

            var stream = new MemoryStream();
            await package.SaveAsAsync(stream);
            stream.Position = 0;

            var fileName = $"Applicants_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpPost("export-excelV2")]
        public async Task<IActionResult> ExportApplicantsV2Async([FromBody] Dictionary<string, List<int>> request)
        {
            request.TryGetValue("applicantIds", out var applicantIds);
            request.TryGetValue("userIds", out var userIds);

            if ((applicantIds == null || applicantIds.Count == 0) &&
                (userIds == null || userIds.Count == 0))
            {
                return BadRequest("ต้องระบุอย่างน้อย applicantId หรือ userId");
            }

            var connectionString = _config.GetConnectionString("DefaultConnection");
            using var connection = new SqlConnection(connectionString);

            var parameters = new DynamicParameters();
            parameters.Add("@ApplicantIDs", JsonSerializer.Serialize(applicantIds ?? []));
            parameters.Add("@UserIDs", JsonSerializer.Serialize(userIds ?? []));

            var rawData = (await connection.QueryAsync<dynamic>(
                "sp_GetApplicantDataV3", parameters, commandType: CommandType.StoredProcedure)).ToList();

            var applicants = rawData.Select(r => (IDictionary<string, object>)r).ToList();

            if (applicants.Count == 0)
                return NotFound("ไม่พบข้อมูลผู้สมัครที่ตรงกับเงื่อนไข");

            var excludedColumns = new HashSet<string>
            {
                "EducationList", "WorkExperienceList", "SkillsList", "UserId", "FilesList"
            };

            var columnOrder = new List<(string Key, string Display)>
            {
                ("CodeMPID", "รหัสพนักงาน"),
                ("Title", "คำนำหน้า"),
                ("FirstNameThai", "ชื่อพนักงาน ภาษาไทย"),
                ("LastNameThai", "นามสกุล ภาษาไทย"),
                ("FirstNameEng", "ชื่อพนักงาน ภาษาอังกฤษ"),
                ("LastNameEng", "นามสกุล ภาษาอังกฤษ"),
                ("BirthDate", "วันเกิด"),
                ("MaritalStatus", "สถานภาพสมรส")
            };

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Applicants");

            for (int col = 0; col < columnOrder.Count; col++)
            {
                ws.Cells[1, col + 1].Value = columnOrder[col].Display;
                ws.Cells[1, col + 1].Style.Font.Bold = true;
            }

            for (int row = 0; row < applicants.Count; row++)
            {
                var item = applicants[row];
                for (int col = 0; col < columnOrder.Count; col++)
                {
                    var key = columnOrder[col].Key;
                    ws.Cells[row + 2, col + 1].Value = item.TryGetValue(key, out object? value) ? value : null;
                }
            }

            ws.Cells.AutoFitColumns();

            var stream = new MemoryStream();
            await package.SaveAsAsync(stream);
            stream.Position = 0;

            var fileName = $"Applicants_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }
}