using System.Data;
using System.Text.Json;
using Dapper;
using JobOnlineAPI.DAL;
using JobOnlineAPI.Models;

namespace JobOnlineAPI.Services
{
    public interface IEmailNotificationService
    {
        Task<int> SendHireToHrEmailsAsync(ApplicantRequestData requestData);
        Task<int> SendManagerEmailsAsync(ApplicantRequestData requestData);
        Task<int> SendHrEmailsAsync(ApplicantRequestData requestData);
        Task<int> SendNotificationEmailsAsync(ApplicantRequestData requestData);
        Task<int> SendApplicationEmailsAsync(IDictionary<string, object?> req, (int ApplicantId, string ApplicantEmail, string HrManagerEmails, string JobManagerEmails, string JobTitle, string CompanyName) dbResult, string applicationFormUri);
    }

    public class EmailNotificationService(
        IEmailService emailService,
        DapperContext context,
        ILogger<EmailNotificationService> logger) : IEmailNotificationService
    {
        private readonly IEmailService _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        private readonly DapperContext _context = context ?? throw new ArgumentNullException(nameof(context));
        private readonly ILogger<EmailNotificationService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        private async Task<IEnumerable<string>> GetEmailRecipientsAsync(int role, string? department = null)
        {
            using var connection = _context.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("@Role", role);
            parameters.Add("@Department", department);
            var staffList = await connection.QueryAsync<StaffEmail>(
                "EXEC sp_GetDateSendEmail @Role, @Department",
                parameters);
            return staffList.Select(staff => staff.Email?.Trim() ?? string.Empty)
                           .Where(email => !string.IsNullOrWhiteSpace(email));
        }

        public async Task<int> SendApplicationEmailsAsync(IDictionary<string, object?> req, (int ApplicantId, string ApplicantEmail, string HrManagerEmails, string JobManagerEmails, string JobTitle, string CompanyName) dbResult, string applicationFormUri)
        {
            var fullNameThai = GetFullName(req);
            var jobTitle = req.TryGetValue("JobTitle", out var jobTitleObj) ? jobTitleObj?.ToString() ?? "-" : "-";
            var typeMail = req.TryGetValue("TypeMail", out var typeMailObj) && typeMailObj != null
                ? typeMailObj is JsonElement t && t.ValueKind == JsonValueKind.String ? t.GetString() : typeMailObj.ToString()
                : null;

            int successCount = 0;

            using var connection = _context.CreateConnection();
            var results = await connection.QueryAsync<StaffEmail>(
                "sp_GetDateSendEmailV3",
                new { JobID = dbResult.ApplicantId },
                commandType: CommandType.StoredProcedure);

            var firstHr = results.FirstOrDefault(x => x.Role == 2);

            if (!string.IsNullOrWhiteSpace(typeMail) && typeMail == "Applicant")
            {
                string managerBody = GenerateManagerEmailBody(fullNameThai, jobTitle);
                foreach (var staff in results)
                {
                    var emailStaff = staff.Email?.Trim();
                    if (string.IsNullOrWhiteSpace(emailStaff))
                        continue;

                    try
                    {
                        await _emailService.SendEmailAsync(emailStaff, "ONEE Jobs - You've got the new candidate update information", managerBody, true);
                        successCount++;
                        _logger.LogInformation("Successfully sent email to {Email}", emailStaff);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send email to {Email}: {Message}", emailStaff, ex.Message);
                    }
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(dbResult.ApplicantEmail))
                {
                    string applicantBody = GenerateEmailBody(true, dbResult.CompanyName, fullNameThai, jobTitle, firstHr, dbResult.ApplicantId, applicationFormUri);
                    try
                    {
                        await _emailService.SendEmailAsync(dbResult.ApplicantEmail, "Application Received", applicantBody, true);
                        successCount++;
                        _logger.LogInformation("Successfully sent email to {Email}", dbResult.ApplicantEmail);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send email to {Email}: {Message}", dbResult.ApplicantEmail, ex.Message);
                    }
                }

                foreach (var staff in results)
                {
                    var emailStaff = staff.Email?.Trim();
                    if (string.IsNullOrWhiteSpace(emailStaff))
                        continue;

                    string managerBody = GenerateEmailBody(false, emailStaff, fullNameThai, jobTitle, null, dbResult.ApplicantId, applicationFormUri);
                    try
                    {
                        await _emailService.SendEmailAsync(emailStaff, "ONEE Jobs - You've got the new candidate", managerBody, true);
                        successCount++;
                        _logger.LogInformation("Successfully sent email to {Email}", emailStaff);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send email to {Email}: {Message}", emailStaff, ex.Message);
                    }
                }
            }

            return successCount;
        }

        public async Task<int> SendHireToHrEmailsAsync(ApplicantRequestData requestData)
        {
            var candidateNames = requestData.Candidates?
                .Select((candidateObj, index) =>
                {
                    var candidateDict = candidateObj as IDictionary<string, object>;
                    string title = candidateDict?.TryGetValue("title", out var titleObj) == true ? titleObj?.ToString() ?? "" : "";
                    string firstNameThai = candidateDict?.TryGetValue("FirstNameThai", out var firstNameObj) == true ? firstNameObj?.ToString() ?? "" : "";
                    string lastNameThai = candidateDict?.TryGetValue("LastNameThai", out var lastNameObj) == true ? lastNameObj?.ToString() ?? "" : "";
                    return $"ลำดับที่ {index + 1}: {title} {firstNameThai} {lastNameThai}".Trim();
                }).ToList() ?? [];

            string candidateNamesString = string.Join("<br>", candidateNames);
            string tel = requestData.Tel ?? "-";

            string hrBody = $@"
                <div style='font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px; font-size: 14px;'>
                    <p style='font-weight: bold; margin: 0 0 10px 0;'>เรียน คุณสมศรี (ผู้จัดการฝ่ายบุคคล)</p>
                    <br>
                    <p style='margin: 0 0 10px 0;'>
                        เรียน ฝ่ายสารรหาบุคคลากร<br>
                        ทาง Hiring Manager แผนก {requestData.NameCon} <br> คุณ {requestData.RequesterName} เบอร์โทร: {tel} อีเมล: {requestData.RequesterMail} <br> 
                        มีการส่งคำร้องให้ท่าน ทำการติดต่อผู้สมัครเพื่อตกลงการจ้างงาน ในตำแหน่ง {requestData.JobTitle}
                    </p>
                    <p style='margin: 0 0 10px 0;'>
                        โดยมี ลำดับรายชื่อการติดต่อดังนี้ <br> {candidateNamesString}
                    </p>
                    <br>
                    <p style='margin: 0 0 10px 0;'><span style='color: red; font-weight: bold;'>*</span> โดยให้ทำการติดต่อ ผู้มัครลำดับที่ 1 ก่อน หากเจรจาไม่สสำเร็จ ให้ทำการติดต่อกับผู้มัครลำดับต่อไป <span style='color: red; font-weight: bold;'>*</span></p>
                    <p style='margin: 0 0 10px 0;'><span style='color: red; font-weight: bold;'>*</span> กรุณา Login เข้าสู่ระบบ https://oneejobs.oneeclick.co:7191/LoginAdmin และไปที่ Menu การว่าจ้าง เพื่อตอบกลับคำขอนี้ <span style='color: red; font-weight: bold;'>*</span></p>
                    <br>
                    <p style='color: red; font-weight: bold;'>**Email อัตโนมัติ โปรดอย่าตอบกลับ**</p>
                </div>";

            var recipients = await GetEmailRecipientsAsync(2);
            return await SendEmailsAsync(recipients, "ONEE Jobs - List of candidates for job interview", hrBody);
        }

        public async Task<int> SendManagerEmailsAsync(ApplicantRequestData requestData)
        {
            var candidateNames = requestData.Candidates?
                .Where(candidateObj =>
                {
                    var candidateDict = candidateObj as IDictionary<string, object>;
                    return candidateDict?.TryGetValue("Status", out var statusObj) == true &&
                           (statusObj?.ToString() == "Success" || statusObj?.ToString() == "Unsuccess" || statusObj?.ToString() == "Cancel");
                })
                .Select((candidateObj, index) =>
                {
                    var candidateDict = candidateObj as IDictionary<string, object>;
                    string title = candidateDict?.TryGetValue("title", out var titleObj) == true ? titleObj?.ToString() ?? "" : "";
                    string firstNameThai = candidateDict?.TryGetValue("FirstNameThai", out var firstNameObj) == true ? firstNameObj?.ToString() ?? "" : "";
                    string lastNameThai = candidateDict?.TryGetValue("LastNameThai", out var lastNameObj) == true ? lastNameObj?.ToString() ?? "" : "";
                    string statusText = candidateDict?.TryGetValue("Status", out var statusObj) == true
                        ? statusObj?.ToString() switch
                        {
                            "Success" => "สำเร็จ",
                            "Unsuccess" => "ต่อรองไม่สำเร็จ",
                            "Cancel" => "ยกเลิก",
                            _ => ""
                        }
                        : "";
                    return $"ลำดับที่ {index + 1}: {title} {firstNameThai} {lastNameThai} สถานะ {statusText}".Trim();
                }).ToList() ?? [];

            string candidateNamesString = string.Join("<br>", candidateNames);

            string hrBody = $@"
                <div style='font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px; font-size: 14px;'>
                    <p style='font-weight: bold; margin: 0 0 10px 0;'>เรียน คุณ {requestData.RequesterName}</p>
                    <p style='font-weight: bold; margin: 0 0 10px 0;'>ทางฝ่าย ฝ่ายสรรหาบุคลากร ขอแจ้งผลการเจรจากับผู้สมัครเพื่อรับเข้าทำงาน โดยมีรายละเอียดดังต่อไปนี้</p>
                    <br>
                    <p style='margin: 0 0 10px 0;'>
                        ตำแหน่ง {requestData.JobTitle}<br>
                        {candidateNamesString}
                    </p>
                    <br>
                    <p style='margin: 0 0 10px 0;'>* สำหรับผู้สมัครที่ต่อรองสำเร็จ ทางฝ่ายฯ จะทำการดำเนินการตามกระบวนการถัดไป เพื่อทำการรับผู้สมัครเข้าเป็นพนักงานและกำหนดวันที่เริ่มงานต่อไป *</p>
                    <p style='color: red; font-weight: bold;'>**อีเมลนี้เป็นข้อความอัตโนมัติ กรุณาอย่าตอบกลับ**</p>
                </div>";

            var recipients = await GetEmailRecipientsAsync(3);
            return await SendEmailsAsync(recipients, "ONEE Jobs - List of candidates for job interview", hrBody);
        }

        public async Task<int> SendHrEmailsAsync(ApplicantRequestData requestData)
        {
            var candidateNames = requestData.Candidates?
                .Select(candidateObj =>
                {
                    var candidateDict = candidateObj as IDictionary<string, object>;
                    string title = candidateDict?.TryGetValue("title", out var titleObj) == true ? titleObj?.ToString() ?? "" : "";
                    string firstNameThai = candidateDict?.TryGetValue("firstNameThai", out var firstNameObj) == true ? firstNameObj?.ToString() ?? "" : "";
                    string lastNameThai = candidateDict?.TryGetValue("lastNameThai", out var lastNameObj) == true ? lastNameObj?.ToString() ?? "" : "";
                    return $"{title} {firstNameThai} {lastNameThai}".Trim();
                }).ToList() ?? [];

            string candidateNamesString = string.Join(" ", candidateNames);

            string hrBody = $@"
                <div style='font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px; font-size: 14px;'>
                    <p style='font-weight: bold; margin: 0 0 10px 0;'>เรียน คุณสมศรี (ผู้จัดการฝ่ายบุคคล)</p>
                    <p style='font-weight: bold; margin: 0 0 10px 0;'>เรื่อง: การเรียกสัมภาษณ์ผู้สมัครตำแหน่ง {requestData.JobTitle}</p>
                    <br>
                    <p style='margin: 0 0 10px 0;'>
                        เรียน ฝ่ายบุคคล<br>
                        ตามที่ได้รับแจ้งข้อมูลผู้สมัครในตำแหน่ง {requestData.JobTitle} จำนวน {candidateNames.Count} ท่าน ผมได้พิจารณาประวัติและคุณสมบัติเบื้องต้นแล้ว และประสงค์จะขอเรียกผู้สมัครดังต่อไปนี้เข้ามาสัมภาษณ์
                    </p>
                    <p style='margin: 0 0 10px 0;'>
                        จากข้อมูลผู้สมัคร ดิฉัน/ผมเห็นว่า {candidateNamesString} มีคุณสมบัติที่เหมาะสมกับตำแหน่งงาน และมีความเชี่ยวชาญในทักษะที่จำเป็นต่อการทำงานในทีมของเรา
                    </p>
                    <br>
                    <p style='margin: 0 0 10px 0;'>ขอความกรุณาฝ่ายบุคคลประสานงานกับผู้สมัครเพื่อนัดหมายการสัมภาษณ์</p>
                    <p style='margin: 0 0 10px 0;'>หากท่านมีข้อสงสัยประการใด กรุณาติดต่อได้ที่เบอร์ด้านล่าง</p>
                    <p style='margin: 0 0 10px 0;'>ขอบคุณสำหรับความช่วยเหลือ</p>
                    <p style='margin: 0 0 10px 0;'>ขอแสดงความนับถือ</p>
                    <p style='margin: 0 0 10px 0;'>{requestData.RequesterName}</p>
                    <p style='margin: 0 0 10px 0;'>{requestData.RequesterPost}</p>
                    <p style='margin: 0 0 10px 0;'>โทร: {requestData.Tel} ต่อ {requestData.TelOff}</p>
                    <p style='margin: 0 0 10px 0;'>อีเมล: {requestData.RequesterMail}</p>
                    <br>
                    <p style='color: red; font-weight: bold;'>**อีเมลนี้เป็นข้อความอัตโนมัติ กรุณาอย่าตอบกลับ**</p>
                </div>";

            var recipients = await GetEmailRecipientsAsync(2);
            return await SendEmailsAsync(recipients, "ONEE Jobs - List of candidates for job interview", hrBody);
        }

        public async Task<int> SendNotificationEmailsAsync(ApplicantRequestData requestData)
        {
            var candidateNames = requestData.Candidates?
                .Select(candidateObj =>
                {
                    var candidateDict = candidateObj as IDictionary<string, object>;
                    string title = candidateDict?.TryGetValue("title", out var titleObj) == true ? titleObj?.ToString() ?? "" : "";
                    string firstNameThai = candidateDict?.TryGetValue("FirstNameThai", out var firstNameObj) == true ? firstNameObj?.ToString() ?? "" : "";
                    string lastNameThai = candidateDict?.TryGetValue("LastNameThai", out var lastNameObj) == true ? lastNameObj?.ToString() ?? "" : "";
                    return $"{title} {firstNameThai} {lastNameThai}".Trim();
                }).ToList() ?? [];

            var candidateEmails = requestData.Candidates?
                .Select(candidateObj =>
                {
                    var candidateDict = candidateObj as IDictionary<string, object>;
                    return candidateDict?.TryGetValue("Email", out var emailObj) == true ? emailObj?.ToString() ?? "" : "";
                }).ToList() ?? [];

            var candidateApplicantIDs = requestData.Candidates?
                .Select(candidateObj =>
                {
                    var candidateDict = candidateObj as IDictionary<string, object>;
                    return candidateDict?.TryGetValue("ApplicantID", out var applicantIdObj) == true ? applicantIdObj?.ToString() ?? "" : "";
                }).ToList() ?? [];

            string candidateName = candidateNames.FirstOrDefault() ?? "ผู้สมัคร";
            string candidateApplicantID = candidateApplicantIDs.FirstOrDefault() ?? "0";

            using var connection = _context.CreateConnection();
            var url = new DynamicParameters();
            url.Add("@ApplicantID", candidateApplicantID);

            var urllist = await connection.QueryAsync<dynamic>(
                "EXEC GetDataForEmailNotiSelectCandidate @ApplicantID",
                url);

            var urlResult = urllist.FirstOrDefault();
            string fromRegis = urlResult != null ? urlResult.LoginUrl?.ToString() ?? "ลิงก์ไม่พร้อมใช้งาน" : "ลิงก์ไม่พร้อมใช้งาน";

            string reqBody = $@"
                <div style='font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px; font-size: 14px;'>
                    <p style='font-weight: bold; margin: 0 0 10px 0;'>เรียน คุณ{candidateName}</p>
                    <p style='font-weight: bold; margin: 0 0 10px 0;'>เรื่อง: ผลสัมภาษณ์ผู้สมัครตำแหน่ง {requestData.JobTitle}</p>
                    <br>
                    <p style='margin: 0 0 10px 0;'>
                        ตามที่ท่านได้สมัครในตำแหน่ง {requestData.JobTitle} ทางบริษัทได้พิจารณาให้คุณผ่านการคัดเลือก กรุณาเข้าไปกรอกรายละเอียดของท่าน ตามลิงก์ด้านล่าง
                    </p>
                    <p style='margin: 0 0 10px 0;'>
                        Click : <a href='{fromRegis}'>{fromRegis}</a>
                    </p>
                    <br>
                    <p style='color: red; font-weight: bold;'>**อีเมลนี้เป็นข้อความอัตโนมัติ กรุณาอย่าตอบกลับ**</p>
                </div>";

            return await SendEmailsAsync(candidateEmails, "ONEE Jobs - List of selected candidates", reqBody);
        }

        private async Task<int> SendEmailsAsync(IEnumerable<string> recipients, string subject, string body)
        {
            int successCount = 0;
            foreach (var email in recipients)
            {
                if (string.IsNullOrWhiteSpace(email))
                    continue;

                try
                {
                    await _emailService.SendEmailAsync(email, subject, body, true);
                    successCount++;
                    _logger.LogInformation("Successfully sent email to {Email}", email);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send email to {Email}: {Message}", email, ex.Message);
                }
            }
            return successCount;
        }

        private static string GenerateManagerEmailBody(string fullNameThai, string jobTitle)
        {
            return $@"
                <div style='font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px; font-size: 14px;'>
                    <p style='font-weight: bold; margin: 0 0 10px 0;'>เรียน ทุกท่าน</p>
                    <p style='font-weight: bold; margin: 0 0 10px 0;'>ผู้สมัคร คุณ {fullNameThai} ตำแหน่ง {jobTitle}</p>
                    <br>
                    <p style='margin: 0 0 10px 0;'>ได้ทำการกรอกข้อมูลในการสมัครงานเพิ่มเติมรอบ ที่ 2 หลังจากที่ได้รับคัดเลือกให้เข้าเป็นพนักงาน เรียบร้อยแล้ว ขั้นตอนถัดไป แผนก HR จะต้องทำการเข้าสู่ระบบและไปที่เมนูการว่าจ้าง เพื่อไปทำการตรวจและยืนยันข้อมูลของผู้สมัคร</p>
                    <br>
                    <p style='color: red; font-weight: bold;'>**อีเมลนี้เป็นข้อความอัตโนมัติ กรุณาอย่าตอบกลับ**</p>
                </div>";
        }

        private static string GenerateEmailBody(bool isApplicant, string recipient, string fullNameThai, string jobTitle, StaffEmail? hr, int applicantId, string applicationFormUri)
        {
            if (isApplicant)
            {
                string companyName = recipient;
                string hrEmail = hr?.Email ?? "-";
                string hrTel = hr?.TELOFF ?? "-";
                string hrName = hr?.NAMETHAI ?? "-";
                return $@"
                    <div style='font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px; font-size: 14px; line-height: 1.6;'>
                        <p style='margin: 0; font-weight: bold;'>{companyName}: ได้รับใบสมัครงานของคุณแล้ว</p>
                        <p style='margin: 0;'>เรียน คุณ {fullNameThai}</p>
                        <p>
                            ขอบคุณสำหรับความสนใจในตำแหน่ง <strong>{jobTitle}</strong> ที่บริษัท <strong>{companyName}</strong> ของเรา<br>
                            เราได้รับใบสมัครของท่านเรียบร้อยแล้ว ทีมงานฝ่ายทรัพยากรบุคคลของเราจะพิจารณาใบสมัครของท่าน และจะติดต่อกลับภายใน 7-14 วันทำการ หากคุณสมบัติของท่านตรงตามที่เรากำลังมองหา<br><br>
                            หากมีข้อสงสัยหรือต้องการข้อมูลเพิ่มเติม สามารถติดต่อเราได้ที่อีเมล 
                            <span style='color: blue;'>{hrEmail}</span> หรือโทร 
                            <span style='color: blue;'>{hrTel}</span><br>
                            ขอบคุณอีกครั้งสำหรับความสนใจร่วมงานกับเรา
                        </p>
                        <p style='margin-top: 30px; margin:0'>ด้วยความเคารพ,</p>
                        <p style='margin: 0;'>{hrName}</p>
                        <p style='margin: 0;'>ฝ่ายทรัพยากรบุคคล</p>
                        <p style='margin: 0;'>{companyName}</p>
                        <br>
                        <p style='color:red; font-weight: bold;'>**อีเมลนี้คือข้อความอัตโนมัติ กรุณาอย่าตอบกลับ**</p>
                    </div>";
            }

            return $@"
                <div style='font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px; font-size: 14px; line-height: 1.6;'>
                    <p style='margin: 0;'>เรียนทุกท่าน</p>
                    <p style='margin: 0;'>เรื่อง: แจ้งข้อมูลผู้สมัครตำแหน่ง <strong>{jobTitle}</strong></p>
                    <p style='margin: 0;'>ทางฝ่ายรับสมัครงานขอแจ้งให้ทราบว่า คุณ <strong>{fullNameThai}</strong> ได้ทำการสมัครงานเข้ามาในตำแหน่ง <strong>{jobTitle}</strong></p>
                    <p style='margin: 0;'>กรุณาคลิก Link:
                        <a target='_blank' href='{applicationFormUri}?id={applicantId}'
                            style='color: #007bff; text-decoration: underline;'>
                            {applicationFormUri}
                        </a>
                        เพื่อดูรายละเอียดและดำเนินการในขั้นตอนต่อไป
                    </p>
                    <br>
                    <p style='color: red; font-weight: bold;'>**อีเมลนี้คือข้อความอัตโนมัติ กรุณาอย่าตอบกลับ**</p>
                </div>";
        }

        private static string GetFullName(IDictionary<string, object?> req)
        {
            req.TryGetValue("FirstNameThai", out var firstNameObj);
            req.TryGetValue("LastNameThai", out var lastNameObj);
            return $"{firstNameObj?.ToString() ?? ""} {lastNameObj?.ToString() ?? ""}".Trim();
        }
    }
}