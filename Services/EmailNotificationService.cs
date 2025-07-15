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
    }
}