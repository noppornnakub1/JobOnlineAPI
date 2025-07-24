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

        [HttpPost("export-excelV2oneFile")]
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
                ("Title", "รหัสคำนำหน้า"),
                ("FirstNameEng", "ชื่อพนักงาน(ภาษาอังกฤษ)"),
                ("FirstNameThai", "ชื่อพนักงาน(ภาษาไทย)"),
                ("LastNameEng", "นามสกุล(ภาษาอังกฤษ)"),
                ("LastNameThai", "นามสกุล(ภาษาไทย)"),
                ("NickNameEng", "ชื่อเล่น(ภาษาอังกฤษ)"),
                ("NickNameThai", "ชื่อเล่น(ภาษาไทย)"),
                ("BirthDate", "วันเกิด"),
                ("MaritalStatus", "สถานภาพสมรส"),
                ("Gender", "เพศ"),
                ("MilitaryStatus", "สถานภาพทหาร"),
                ("JoinDate", "วันที่เข้างาน"),
                ("RetirementDate", "วันที่เกษียณอายุ"),
                ("OrgCode", "รหัสสังกัด"),
                ("PositionCode", "รหัสตำแหน่ง"),
                ("EmployeeLevel", "ระดับพนักงาน"),
                ("EmployeeStatus", "สถานภาพพนักงาน"),
                ("TerminationDate", "วันที่พ้นสภาพ"),
                ("Flag", "Flag"),
                ("TimeTracking", "การบันทึกเวลา"),
                ("WorkplaceCode", "รหัสสถานที่ทำงาน"),
                ("EmploymentType", "ประเภทการจ้างงาน"),
                ("PayGroupCode", "รหัสกลุ่มการจ่ายเงินเดือน"),
                ("EmployeeType", "ประเภทพนักงาน"),
                ("JobGroupCode", "รหัสกลุ่มงาน"),
                ("JobFunctionCode", "รหัสลักษณะงาน"),
                ("JobGrade", "Job Grade"),
                ("GLGroupCode", "รหัสกลุ่ม GL"),
                ("LevelStartDate", "วันที่เริ่มระดับพนักงาน"),
                ("PositionStartDate", "วันที่เริ่มตำแหน่ง"),
                ("RankStartDate", "วันที่เริ่มระดับ"),
                ("ProbationDueDate", "วันที่ครบกำหนดทดลองงาน"),
                ("ConfirmedDate", "วันที่บรรจุ"),
                ("EmploymentDuration", "กำหนดการจ้างงาน(เดือน)"),
                ("InternalPhone", "โทรศัพท์ติดต่อภายใน"),
                ("MobilePhone", "โทรศัพท์มือถือ"),
                ("Email", "ชื่ออีเมล์"),
                ("LineID", "Line ID"),
                ("ApplicantNo", "เลขที่ใบสมัคร"),
                ("OldEmployeeCode", "รหัสพนักงานเดิม"),
                ("EmployeeCategory", "ประเภทพนักงาน"),
                ("DisabilityRegNo", "เลขที่ทะเบียนคนพิการ"),
                ("DisabilityType", "ประเภทความพิการ"),
                ("DisabilityIssueDate", "วันที่ออกสมุดคนพิการ"),
                ("DisabilityExpiryDate", "วันที่หมดอายุ"),
                ("DisabilityDescription", "ลักษณะความพิการ"),
                ("TransportationType", "ประเภทการเดินทาง"),
                ("DistanceToWork", "ระยะทางจากบ้านถึงที่ทำงาน"),
                ("CarLicenseNo", "หมายเลขทะเบียนรถ"),
                ("FuelType", "ประเภทเชื้อเพลิง"),
                ("CarRouteCode", "รหัสสายรถ"),
                ("CarStopCode", "รหัสจุดจอดของรถ"),
                ("EmailLanguage", "Get E-Mail Langauge"),
                ("ConsentStatus", "สถานะยินยอมให้เปิดเผยข้อมูล"),
                ("ConsentDate", "วันที่ยินยอม")
            };

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("ข้อมูลพนักงาน 1");
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
                    // ws.Cells[row + 2, col + 1].Value = item.TryGetValue(key, out object? value) ? value : null;
                    ws.Cells[row + 2, col + 1].Value = item.TryGetValue(key, out object? value) ? value : "null";
                }
            }

            ws.Cells.AutoFitColumns();

            var columnOrderSheet2 = new List<(string Key, string Display)>
            {
                ("CodeMPID", "รหัสพนักงาน"),
                ("RegisteredAddressEng", "ที่อยู่ตามทะเบียนบ้าน (ภาษาอังกฤษ)"),
                ("RegisteredAddressThai", "ที่อยู่ตามทะเบียนบ้าน (ภาษาไทย)"),
                ("RegisteredSubDistrictCode", "รหัสตำบลตามทะเบียนบ้าน"),
                ("RegisteredDistrictCode", "รหัสอำเภอตามทะเบียนบ้าน"),
                ("RegisteredProvinceCode", "รหัสจังหวัดตามทะเบียนบ้าน"),
                ("RegisteredCountryCode", "รหัสประเทศตามทะเบียนบ้าน"),
                ("RegisteredPostalCode", "รหัสไปรษณีย์ตามทะเบียนบ้าน"),
                ("ContactAddressEng", "ที่อยู่ที่ติดต่อได้ (ภาษาอังกฤษ)"),
                ("ContactAddressThai", "ที่อยู่ที่ติดต่อได้ (ภาษาไทย)"),
                ("ContactSubDistrictCode", "รหัสตำบลที่ติดต่อ"),
                ("ContactDistrictCode", "รหัสอำเภอที่ติดต่อ"),
                ("ContactProvince", "จังหวัดตามที่อยู่ที่ติดต่อได้"),
                ("ContactCountryCode", "รหัสประเทศที่ติดต่อได้"),
                ("ContactPostalCode", "รหัสไปรษณีย์ที่ติดต่อได้"),
                ("ContactPhone", "เบอร์โทรศัทพ์ที่ติดต่อได้"),
                ("BloodGroup", "กลุ่มเลือด"),
                ("Weight", "น้ำหนัก"),
                ("Height", "ส่วนสูง"),
                ("Religion", "ศาสนา"),
                ("Race", "เชื้อชาติ"),
                ("Nationality", "สัญชาติ"),
                ("BirthPlace", "ภูมิลำเนาเดิม (สถานที่เกิด)"),
                ("NationalID", "บัตรประจำตัวประชาชน"),
                ("NationalIDIssueDistrict", "ออกบัตรประจำตัวประชาชนโดยเขต"),
                ("NationalIDProvince", "จังหวัดที่ออกบัตร"),
                ("NationalIDExpiryDate", "วันที่หมดอายุบัตรประชาชน"),
                ("SocialSecurityHospital", "โรงพยาบาลประกันสังคม"),
                ("DrivingLicenseNumber", "เลขที่ใบขับขี่"),
                ("DrivingLicenseExpiryDate", "วันที่หมดอายุใบขับขี่"),
                ("PassportNumber", "เลขที่หนังสือเดินทาง (พาสปอร์ต)"),
                ("PassportExpiryDate", "วันที่หมดอายุหนังสือเดินทาง (พาสปอร์ต)"),
                ("VisaNumber", "เลขที่ Visa"),
                ("VisaExpiryDate", "วันที่หมดอายุ VISA"),
                ("WorkPermitNumber", "เลขที่ใบอนุญาตทำงาน"),
                ("WorkPermitIssueDate", "วันที่ออกใบอนุญาต ณ วันที่"),
                ("WorkPermitExpiryDate", "วันที่หมดอายุ ใบอนุญาติทำงาน")
            };

            var ws2 = package.Workbook.Worksheets.Add("ข้อมูลพนักงาน 2");

            for (int col = 0; col < columnOrderSheet2.Count; col++)
            {
                ws2.Cells[1, col + 1].Value = columnOrderSheet2[col].Display;
                ws2.Cells[1, col + 1].Style.Font.Bold = true;
            }

            for (int row = 0; row < applicants.Count; row++)
            {
                var item = applicants[row];
                for (int col = 0; col < columnOrderSheet2.Count; col++)
                {
                    var key = columnOrderSheet2[col].Key;
                    ws2.Cells[row + 2, col + 1].Value = item.TryGetValue(key, out object? value)
                        ? value ?? "null"
                        : "null";
                }
            }

            ws2.Cells.AutoFitColumns();

            var columnOrderSheet3 = new List<(string Key, string Display)>
            {
                ("CodeMPID", "รหัสพนักงาน"),
                ("IncomeCurrency", "สกุลเงินของรายได้"),
                ("IncomeAmount1", "จำนวนเงินรายได้(ลำดับที่ 1)"),
                ("IncomeAmount2", "จำนวนเงินรายได้(ลำดับที่ 2)"),
                ("IncomeAmount3", "จำนวนเงินรายได้(ลำดับที่ 3)"),
                ("IncomeAmount4", "จำนวนเงินรายได้(ลำดับที่ 4)"),
                ("IncomeAmount5", "จำนวนเงินรายได้(ลำดับที่ 5)"),
                ("IncomeAmount6", "จำนวนเงินรายได้(ลำดับที่ 6)"),
                ("IncomeAmount7", "จำนวนเงินรายได้(ลำดับที่ 7)"),
                ("IncomeAmount8", "จำนวนเงินรายได้(ลำดับที่ 8)"),
                ("IncomeAmount9", "จำนวนเงินรายได้(ลำดับที่ 9)"),
                ("IncomeAmount10", "จำนวนเงินรายได้(ลำดับที่ 10)"),
                ("TaxpayerID", "เลขประจำตัวผู้เสียภาษี"),
                ("SocialSecurityNumber", "เลขประจำตัวผู้ประกันตน"),
                ("WithholdingType", "1-หักณที่จ่าย 2-บริษัทออกให้"),
                ("TaxFilingType", "การยืนแบบคำนวนภาษี 1-แยกยื่น, 2-รวมยื่น"),
                ("IncomeType", "ประเภทเงินได้"),
                ("BankCode1", "รหัสธนาคาร"),
                ("BankAccount1", "เลขที่บัญชี"),
                ("BankTransferPercent", "% โอนเข้าบัญชี"),
                ("BankTransferMaxAmount", "จำนวนเงินสูงสุดที่โอนเข้า"),
                ("BankCode2", "รหัสธนาคาร (ลำดับที่ 2)"),
                ("BankAccount2", "เลขที่บัญชี (ลำดับที่ 2)"),
                ("AccumulatedIncome", "เงินได้สะสมยกมา"),
                ("AccumulatedTax", "ภาษีสะสมยกมา"),
                ("AccumulatedProvidentFund", "เงินสะสมกองทุนสะสมยกมา"),
                ("AccumulatedSocialSecurity", "เงินสะสมประกันสังคมยกมา"),
                ("SpouseAccumulatedIncome", "เงินได้สะสมยกมาของคู่สมรส"),
                ("SpouseAccumulatedTax", "ภาษีสะสมยกมาของคู่สมรส"),
                ("SpouseSocialSecurity", "เงินสะสมประกันสังคมของคู่สมรส"),
                ("SpouseProvidentFund", "เงินสะสมกองทุนสำรองของคู่สมรส"),
                ("BankBranch1", "สาขาของธนาคาร"),
                ("BankBranch2", "สาขาของธนาคาร (ลำดับที่ 2)"),
                ("PostProbationAdjustment", "จำนวนเงินที่ปรับหลังทดลองงาน"),
                ("ChildrenBefore2018", "จำนวนบุตรเกิดก่อนปี 2561"),
                ("ChildrenAfter2018", "จำนวนบุตรเกิดตั้งแต่ปี 2561"),
                ("AdoptedChildren", "จำนวนบุตรบุญธรรม"),
                ("DisabledPersonsSupport", "จำนวนที่อุปการะเลี้ยงดูคนพิการ"),
                ("PayslipCondition", "เงื่อนไข Payslip")
            };

            var ws3 = package.Workbook.Worksheets.Add("ข้อมูลพนักงาน 3");

            for (int col = 0; col < columnOrderSheet3.Count; col++)
            {
                ws3.Cells[1, col + 1].Value = columnOrderSheet3[col].Display;
                ws3.Cells[1, col + 1].Style.Font.Bold = true;
            }

            for (int row = 0; row < applicants.Count; row++)
            {
                var item = applicants[row];
                for (int col = 0; col < columnOrderSheet3.Count; col++)
                {
                    var key = columnOrderSheet3[col].Key;
                    ws3.Cells[row + 2, col + 1].Value = item.TryGetValue(key, out object? value)
                        ? value ?? "null"
                        : "null";
                }
            }

            ws3.Cells.AutoFitColumns();


            var stream = new MemoryStream();
            await package.SaveAsAsync(stream);
            stream.Position = 0;

            var fileName = $"Applicants_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    


        [HttpPost("export-excelV2")]
        public async Task<IActionResult> ExportApplicantsV3Async([FromBody] Dictionary<string, List<int>> request)
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

            // ==== ใส่ columnOrder ทั้ง 3 ชุดตามที่จารสร้างไว้ ====
            var columnOrderSheet1 = new List<(string Key, string Display)>
            {
                ("CodeMPID", "รหัสพนักงาน"),
                ("Title", "รหัสคำนำหน้า"),
                ("FirstNameEng", "ชื่อพนักงาน(ภาษาอังกฤษ)"),
                ("FirstNameThai", "ชื่อพนักงาน(ภาษาไทย)"),
                ("LastNameEng", "นามสกุล(ภาษาอังกฤษ)"),
                ("LastNameThai", "นามสกุล(ภาษาไทย)"),
                ("NickNameEng", "ชื่อเล่น(ภาษาอังกฤษ)"),
                ("NickNameThai", "ชื่อเล่น(ภาษาไทย)"),
                ("BirthDate", "วันเกิด"),
                ("MaritalStatus", "สถานภาพสมรส"),
                ("Gender", "เพศ"),
                ("MilitaryStatus", "สถานภาพทหาร"),
                ("JoinDate", "วันที่เข้างาน"),
                ("RetirementDate", "วันที่เกษียณอายุ"),
                ("OrgCode", "รหัสสังกัด"),
                ("PositionCode", "รหัสตำแหน่ง"),
                ("EmployeeLevel", "ระดับพนักงาน"),
                ("EmployeeStatus", "สถานภาพพนักงาน"),
                ("TerminationDate", "วันที่พ้นสภาพ"),
                ("Flag", "Flag"),
                ("TimeTracking", "การบันทึกเวลา"),
                ("WorkplaceCode", "รหัสสถานที่ทำงาน"),
                ("EmploymentType", "ประเภทการจ้างงาน"),
                ("PayGroupCode", "รหัสกลุ่มการจ่ายเงินเดือน"),
                ("EmployeeType", "ประเภทพนักงาน"),
                ("JobGroupCode", "รหัสกลุ่มงาน"),
                ("JobFunctionCode", "รหัสลักษณะงาน"),
                ("JobGrade", "Job Grade"),
                ("GLGroupCode", "รหัสกลุ่ม GL"),
                ("LevelStartDate", "วันที่เริ่มระดับพนักงาน"),
                ("PositionStartDate", "วันที่เริ่มตำแหน่ง"),
                ("RankStartDate", "วันที่เริ่มระดับ"),
                ("ProbationDueDate", "วันที่ครบกำหนดทดลองงาน"),
                ("ConfirmedDate", "วันที่บรรจุ"),
                ("EmploymentDuration", "กำหนดการจ้างงาน(เดือน)"),
                ("InternalPhone", "โทรศัพท์ติดต่อภายใน"),
                ("MobilePhone", "โทรศัพท์มือถือ"),
                ("Email", "ชื่ออีเมล์"),
                ("LineID", "Line ID"),
                ("ApplicantNo", "เลขที่ใบสมัคร"),
                ("OldEmployeeCode", "รหัสพนักงานเดิม"),
                ("EmployeeCategory", "ประเภทพนักงาน"),
                ("DisabilityRegNo", "เลขที่ทะเบียนคนพิการ"),
                ("DisabilityType", "ประเภทความพิการ"),
                ("DisabilityIssueDate", "วันที่ออกสมุดคนพิการ"),
                ("DisabilityExpiryDate", "วันที่หมดอายุ"),
                ("DisabilityDescription", "ลักษณะความพิการ"),
                ("TransportationType", "ประเภทการเดินทาง"),
                ("DistanceToWork", "ระยะทางจากบ้านถึงที่ทำงาน"),
                ("CarLicenseNo", "หมายเลขทะเบียนรถ"),
                ("FuelType", "ประเภทเชื้อเพลิง"),
                ("CarRouteCode", "รหัสสายรถ"),
                ("CarStopCode", "รหัสจุดจอดของรถ"),
                ("EmailLanguage", "Get E-Mail Langauge"),
                ("ConsentStatus", "สถานะยินยอมให้เปิดเผยข้อมูล"),
                ("ConsentDate", "วันที่ยินยอม")
            };
            var columnOrderSheet2 = new List<(string Key, string Display)>
            {
                ("CodeMPID", "รหัสพนักงาน"),
                ("RegisteredAddressEng", "ที่อยู่ตามทะเบียนบ้าน (ภาษาอังกฤษ)"),
                ("RegisteredAddressThai", "ที่อยู่ตามทะเบียนบ้าน (ภาษาไทย)"),
                ("RegisteredSubDistrictID", "รหัสตำบลตามทะเบียนบ้าน"),
                ("RegisteredDistrictID", "รหัสอำเภอตามทะเบียนบ้าน"),
                ("RegisteredProvinceID", "รหัสจังหวัดตามทะเบียนบ้าน"),
                ("RegisteredCountryCode", "รหัสประเทศตามทะเบียนบ้าน"),
                ("RegisteredPostalCode", "รหัสไปรษณีย์ตามทะเบียนบ้าน"),
                ("ContactAddressEng", "ที่อยู่ที่ติดต่อได้ (ภาษาอังกฤษ)"),
                ("CurrentAddress", "ที่อยู่ที่ติดต่อได้ (ภาษาไทย)"),
                ("CurrentSubDistrictID", "รหัสตำบลที่ติดต่อ"),
                ("CurrentDistrictID", "รหัสอำเภอที่ติดต่อ"),
                ("CurrentProvinceID", "จังหวัดตามที่อยู่ที่ติดต่อได้"),
                ("ContactCountryCode", "รหัสประเทศที่ติดต่อได้"),
                ("CurrentPostalCode", "รหัสไปรษณีย์ที่ติดต่อได้"),
                ("ContactPhone", "เบอร์โทรศัทพ์ที่ติดต่อได้"),
                ("BloodGroup", "กลุ่มเลือด"),
                ("Weight", "น้ำหนัก"),
                ("Height", "ส่วนสูง"),
                ("Religion", "ศาสนา"),
                ("Race", "เชื้อชาติ"),
                ("Nationality", "สัญชาติ"),
                ("BirthPlace", "ภูมิลำเนาเดิม (สถานที่เกิด)"),
                ("CitizenID", "บัตรประจำตัวประชาชน"),
                ("NationalIDIssueDistrict", "ออกบัตรประจำตัวประชาชนโดยเขต"),
                ("NationalIDProvince", "จังหวัดที่ออกบัตร"),
                ("NationalIDExpiryDate", "วันที่หมดอายุบัตรประชาชน"),
                ("SocialSecurityHospital", "โรงพยาบาลประกันสังคม"),
                ("DrivingLicenseNumber", "เลขที่ใบขับขี่"),
                ("DrivingLicenseExpiryDate", "วันที่หมดอายุใบขับขี่"),
                ("PassportNumber", "เลขที่หนังสือเดินทาง (พาสปอร์ต)"),
                ("PassportExpiryDate", "วันที่หมดอายุหนังสือเดินทาง (พาสปอร์ต)"),
                ("VisaNumber", "เลขที่ Visa"),
                ("VisaExpiryDate", "วันที่หมดอายุ VISA"),
                ("WorkPermitNumber", "เลขที่ใบอนุญาตทำงาน"),
                ("WorkPermitIssueDate", "วันที่ออกใบอนุญาต ณ วันที่"),
                ("WorkPermitExpiryDate", "วันที่หมดอายุ ใบอนุญาติทำงาน")
            };
            var columnOrderSheet3 = new List<(string Key, string Display)>
            {
                ("CodeMPID", "รหัสพนักงาน"),
                ("IncomeCurrency", "สกุลเงินของรายได้"),
                ("IncomeAmount1", "จำนวนเงินรายได้(ลำดับที่ 1)"),
                ("IncomeAmount2", "จำนวนเงินรายได้(ลำดับที่ 2)"),
                ("IncomeAmount3", "จำนวนเงินรายได้(ลำดับที่ 3)"),
                ("IncomeAmount4", "จำนวนเงินรายได้(ลำดับที่ 4)"),
                ("IncomeAmount5", "จำนวนเงินรายได้(ลำดับที่ 5)"),
                ("IncomeAmount6", "จำนวนเงินรายได้(ลำดับที่ 6)"),
                ("IncomeAmount7", "จำนวนเงินรายได้(ลำดับที่ 7)"),
                ("IncomeAmount8", "จำนวนเงินรายได้(ลำดับที่ 8)"),
                ("IncomeAmount9", "จำนวนเงินรายได้(ลำดับที่ 9)"),
                ("IncomeAmount10", "จำนวนเงินรายได้(ลำดับที่ 10)"),
                ("TaxpayerID", "เลขประจำตัวผู้เสียภาษี"),
                ("SocialSecurityNumber", "เลขประจำตัวผู้ประกันตน"),
                ("WithholdingType", "1-หักณที่จ่าย 2-บริษัทออกให้"),
                ("TaxFilingType", "การยืนแบบคำนวนภาษี 1-แยกยื่น, 2-รวมยื่น"),
                ("IncomeType", "ประเภทเงินได้"),
                ("BankCode1", "รหัสธนาคาร"),
                ("BankAccount1", "เลขที่บัญชี"),
                ("BankTransferPercent", "% โอนเข้าบัญชี"),
                ("BankTransferMaxAmount", "จำนวนเงินสูงสุดที่โอนเข้า"),
                ("BankCode2", "รหัสธนาคาร (ลำดับที่ 2)"),
                ("BankAccount2", "เลขที่บัญชี (ลำดับที่ 2)"),
                ("AccumulatedIncome", "เงินได้สะสมยกมา"),
                ("AccumulatedTax", "ภาษีสะสมยกมา"),
                ("AccumulatedProvidentFund", "เงินสะสมกองทุนสะสมยกมา"),
                ("AccumulatedSocialSecurity", "เงินสะสมประกันสังคมยกมา"),
                ("SpouseAccumulatedIncome", "เงินได้สะสมยกมาของคู่สมรส"),
                ("SpouseAccumulatedTax", "ภาษีสะสมยกมาของคู่สมรส"),
                ("SpouseSocialSecurity", "เงินสะสมประกันสังคมของคู่สมรส"),
                ("SpouseProvidentFund", "เงินสะสมกองทุนสำรองของคู่สมรส"),
                ("BankBranch1", "สาขาของธนาคาร"),
                ("BankBranch2", "สาขาของธนาคาร (ลำดับที่ 2)"),
                ("PostProbationAdjustment", "จำนวนเงินที่ปรับหลังทดลองงาน"),
                ("ChildrenBefore2018", "จำนวนบุตรเกิดก่อนปี 2561"),
                ("ChildrenAfter2018", "จำนวนบุตรเกิดตั้งแต่ปี 2561"),
                ("AdoptedChildren", "จำนวนบุตรบุญธรรม"),
                ("DisabledPersonsSupport", "จำนวนที่อุปการะเลี้ยงดูคนพิการ"),
                ("PayslipCondition", "เงื่อนไข Payslip")
            };

            // ==== สร้างไฟล์ Excel ทีละชุดเป็น Byte Array ====
            byte[] excelFile1 = GenerateExcelFile(applicants, columnOrderSheet1, "PM-TEMPLOY 1");
            byte[] excelFile2 = GenerateExcelFile(applicants, columnOrderSheet2, "PM-TEMPLOY 2");
            byte[] excelFile3 = GenerateExcelFile(applicants, columnOrderSheet3, "PM-TEMPLOY 3");

            // ==== สร้าง ZIP ไฟล์ ====
            using var zipStream = new MemoryStream();
            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                var entry1 = archive.CreateEntry("PM-TEMPLOY1.xlsx");
                using (var entryStream = entry1.Open())
                {
                    entryStream.Write(excelFile1, 0, excelFile1.Length);
                }

                var entry2 = archive.CreateEntry("PM-TEMPLOY2.xlsx");
                using (var entryStream = entry2.Open())
                {
                    entryStream.Write(excelFile2, 0, excelFile2.Length);
                }

                var entry3 = archive.CreateEntry("PM-TEMPLOY3.xlsx");
                using (var entryStream = entry3.Open())
                {
                    entryStream.Write(excelFile3, 0, excelFile3.Length);
                }
            }

            zipStream.Position = 0;
            var zipFileName = $"PM-TEMPLOY_{DateTime.Now:yyyyMMdd_HHmmss}.zip";

            return File(
                fileContents: zipStream.ToArray(),
                contentType: "application/zip",
                fileDownloadName: zipFileName
            );
        }

        private byte[] GenerateExcelFile(List<IDictionary<string, object>> applicants, List<(string Key, string Display)> columnOrder, string sheetName)
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
                    ws.Cells[row + 2, col + 1].Value = item.TryGetValue(key, out object? value)
                        ? value ?? "null"
                        : "null";
                }
            }

            ws.Cells.AutoFitColumns();
            return package.GetAsByteArray();
        }

    }
}