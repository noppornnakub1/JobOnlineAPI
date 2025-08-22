using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using System.Data;
using System.Data.SqlClient;
using Dapper;
using System.Text.Json;
using System.IO.Compression;

namespace JobOnlineAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExportController(IConfiguration config) : ControllerBase
    {
        private readonly IConfiguration _config = config;

        [HttpPost("export-excelV2")]
        public async Task<IActionResult> ExportApplicantsAsync([FromBody] Dictionary<string, List<int>> request)
        {
            request.TryGetValue("applicantIds", out var applicantIds);
            request.TryGetValue("userIds", out var userIds);
            if ((applicantIds == null || applicantIds.Count == 0) && (userIds == null || userIds.Count == 0))
            {
                return BadRequest("ต้องระบุอย่างน้อย applicantId หรือ userId");
            }

            // ตรวจสอบ input
            if (applicantIds != null && applicantIds.Any(id => id <= 0))
                return BadRequest("Applicant IDs ต้องเป็นจำนวนเต็มบวก");
            if (userIds != null && userIds.Any(id => id <= 0))
                return BadRequest("User IDs ต้องเป็นจำนวนเต็มบวก");

            using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@ApplicantIDs", JsonSerializer.Serialize(applicantIds ?? []));
                parameters.Add("@UserIDs", JsonSerializer.Serialize(userIds ?? []));

                using var grid = await connection.QueryMultipleAsync(
                    "sp_GetDataExportExcel",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var rawApplicants = (await grid.ReadAsync<dynamic>()).ToList();
                var applicants = rawApplicants.Select(r => (IDictionary<string, object>)r).ToList();
                if (applicants.Count == 0)
                    return NotFound("ไม่พบข้อมูลผู้สมัครที่ตรงกับเงื่อนไข");
                var sheet1Defs = (await grid.ReadAsync<ColumnDef>()).OrderBy(x => x.Order).ToList();
                var sheet2Defs = (await grid.ReadAsync<ColumnDef>()).OrderBy(x => x.Order).ToList();
                var sheet3Defs = (await grid.ReadAsync<ColumnDef>()).OrderBy(x => x.Order).ToList();

                // ตรวจสอบความถูกต้องของ column definitions
                if (sheet1Defs.Count == 0 || sheet1Defs.Any(d => string.IsNullOrEmpty(d.Key) || string.IsNullOrEmpty(d.Display)))
                    return StatusCode(500, "กำหนดคอลัมน์สำหรับ sheet 1 ไม่ถูกต้อง");
                if (sheet2Defs.Count == 0 || sheet2Defs.Any(d => string.IsNullOrEmpty(d.Key) || string.IsNullOrEmpty(d.Display)))
                    return StatusCode(500, "กำหนดคอลัมน์สำหรับ sheet 2 ไม่ถูกต้อง");
                if (sheet3Defs.Count == 0 || sheet3Defs.Any(d => string.IsNullOrEmpty(d.Key) || string.IsNullOrEmpty(d.Display)))
                    return StatusCode(500, "กำหนดคอลัมน์สำหรับ sheet 3 ไม่ถูกต้อง");

                var columnOrderSheet1 = sheet1Defs.Select(x => (x.Key, x.Display)).ToList();
                var columnOrderSheet2 = sheet2Defs.Select(x => (x.Key, x.Display)).ToList();
                var columnOrderSheet3 = sheet3Defs.Select(x => (x.Key, x.Display)).ToList();
                byte[] excelFile1 = GenerateExcelFile(applicants, columnOrderSheet1, "PM-TEMPLOY 1");
                byte[] excelFile2 = GenerateExcelFile(applicants, columnOrderSheet2, "PM-TEMPLOY 2");
                byte[] excelFile3 = GenerateExcelFile(applicants, columnOrderSheet3, "PM-TEMPLOY 3");

                using var zipStream = new MemoryStream();
                using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: false))
                {
                    try
                    {
                        using (var s1 = archive.CreateEntry("PM-TEMPLOY1.xlsx").Open()) s1.Write(excelFile1, 0, excelFile1.Length);
                        using (var s2 = archive.CreateEntry("PM-TEMPLOY2.xlsx").Open()) s2.Write(excelFile2, 0, excelFile2.Length);
                        using var s3 = archive.CreateEntry("PM-TEMPLOY3.xlsx").Open(); s3.Write(excelFile3, 0, excelFile3.Length);
                    }
                    catch (IOException ex)
                    {
                        return StatusCode(500, $"ข้อผิดพลาดในการสร้าง ZIP: {ex.Message}");
                    }
                }

                var zipFileName = $"PM-TEMPLOY_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N")[..8]}.zip";
                return File(zipStream.ToArray(), "application/zip", zipFileName);
            }
            catch (SqlException ex)
            {
                // Log error
                return StatusCode(500, $"ข้อผิดพลาดจากฐานข้อมูล: {ex.Message}");
            }
        }

        public sealed class ColumnDef
        {
            public int SheetNo { get; set; }
            public int Order { get; set; }
            public string Key { get; set; } = "";
            public string Display { get; set; } = "";
        }

        private static byte[] GenerateExcelFile(List<IDictionary<string, object>> applicants, List<(string Key, string Display)> columnOrder, string sheetName)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add(sheetName);
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
                    var cellValue = item.TryGetValue(key, out object? value) ? value : null;
                    ws.Cells[row + 2, col + 1].Value = string.IsNullOrWhiteSpace(cellValue?.ToString()) ? null : cellValue;
                }
            }
            ws.Cells.AutoFitColumns();
            return package.GetAsByteArray();
        }
    }
}