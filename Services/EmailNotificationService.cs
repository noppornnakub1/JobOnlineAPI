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
        Task<int> SendApplicationEmailsAsync(IDictionary<string, object?> req, (int ApplicantId, string ApplicantEmail, string HrManagerEmails, string JobManagerEmails, string JobTitle, string CompanyName, int OutJobID) dbResult, string applicationFormUri);
        Task<int> SendEmailsJobsStatusAsync(int JobID);
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
            //sp_GetDateSendEmail 
            var staffList = await connection.QueryAsync<StaffEmail>(
                "EXEC sp_GetDataSendEmailRecipients  @Role, @Department",
                parameters);
            return staffList.Select(staff => staff.Email?.Trim() ?? string.Empty)
                           .Where(email => !string.IsNullOrWhiteSpace(email));
        }

        public async Task<int> SendApplicationEmailsAsync(IDictionary<string, object?> req, (int ApplicantId, string ApplicantEmail, string HrManagerEmails, string JobManagerEmails, string JobTitle, string CompanyName, int OutJobID) dbResult, string applicationFormUri)
        {
            var fullNameThai = GetFullName(req);
            var jobTitle = req.TryGetValue("JobTitle", out var jobTitleObj) ? jobTitleObj?.ToString() ?? "-" : "-";
            var typeMail = req.TryGetValue("TypeMail", out var typeMailObj) && typeMailObj != null
                ? typeMailObj is JsonElement t && t.ValueKind == JsonValueKind.String ? t.GetString() : typeMailObj.ToString()
                : null;

            int successCount = 0;
            //"sp_GetDateSendEmailV3"
            using var connection = _context.CreateConnection();
            var results = await connection.QueryAsync<StaffEmail>(
                "sp_GetDataSendEmailRegister",
                new { JobID = dbResult.OutJobID },
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
                        await _emailService.SendEmailAsync(emailStaff, "ONEE Jobs - You've got the new candidate update information", managerBody, true, "Register", null);
                        successCount++;
                        _logger.LogInformation("Successfully sent email to {Email}", emailStaff);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send email to {Email}: {Message}", emailStaff, ex.Message);
                    }
                }
            } 
            else if (!string.IsNullOrWhiteSpace(typeMail) && typeMail == "HRConfirmed")
            {
                string managerBody = GenerateManagerEmailBody(fullNameThai, jobTitle);
                foreach (var staff in results)
                {
                    var emailStaff = staff.Email?.Trim();
                    if (string.IsNullOrWhiteSpace(emailStaff))
                        continue;

                    try
                    {
                        managerBody = $@"
                            <div style='font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px; font-size: 14px;'>
                                <p style='font-weight: bold; margin: 0 0 10px 0;'>เรียน ทุกท่าน</p>
                                <p>ขอแจ้งให้ทราบว่า ขณะนี้ได้ดำเนินการสรรหาและตกลงกับผู้สมัคร {fullNameThai} เรียบร้อยแล้วค่ะ</p>
                                <p style='font-weight: bold; margin: 0 0 10px 0;'>ผู้สมัคร คุณ {fullNameThai} ตำแหน่ง {jobTitle}</p>
                                <br>
                                <p style='margin-top: 30px; margin:0'>ด้วยความเคารพ,</p>
                                <p style='margin: 0;'>ฝ่ายทรัพยากรบุคคล</p>
                                <br>
                                <p style='color: red; font-weight: bold;'>**อีเมลนี้เป็นข้อความอัตโนมัติ กรุณาอย่าตอบกลับ**</p>
                            </div>";
                        string SubjectMail = $@"แจ้งผลการสรรหา - คุณ {fullNameThai}";
                        await _emailService.SendEmailAsync(emailStaff, SubjectMail, managerBody, true, "Register", null);
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
                        await _emailService.SendEmailAsync(dbResult.ApplicantEmail, "Application Received", applicantBody, true, "Register", null);
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
                        await _emailService.SendEmailAsync(emailStaff, "ONEE Jobs - You've got the new candidate", managerBody, true, "Register", null);
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
                .Select((candidate, index) =>
                    $"ลำดับที่ {index + 1}: {candidate.Title} {candidate.FirstNameThai} {candidate.LastNameThai}".Trim())
                .ToList() ?? [];

            string candidateNamesString = string.Join("<br>", candidateNames);
            string tel = requestData.Tel ?? "-";

            string hrBody = $@"
            <div style='font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px; font-size: 14px;'>
                <p style='margin: 0 0 10px 0;'>
                    เรียน ฝ่ายสารรหาบุคคลากร<br>
                    ทาง Hiring Manager แผนก {requestData.NameCon} <br> คุณ {requestData.RequesterName} เบอร์โทร: {tel} อีเมล: {requestData.RequesterMail} <br> 
                    มีการส่งคำร้องให้ท่าน ทำการติดต่อผู้สมัครเพื่อตกลงการจ้างงาน ในตำแหน่ง {requestData.JobTitle}
                </p>
                <p style='margin: 0 0 10px 0;'>
                    โดยมี ลำดับรายชื่อการติดต่อดังนี้ <br> {candidateNamesString}
                </p>
                <br>
                <p style='margin: 0 0 10px 0;'><span style='color: red; font-weight: bold;'>*</span> โดยให้ทำการติดต่อ ผู้มัครลำดับที่ 1 ก่อน หากเจรจาไม่สำเร็จ ให้ทำการติดต่อกับผู้มัครลำดับต่อไป <span style='color: red; font-weight: bold;'>*</span></p>
                <p style='margin: 0 0 10px 0;'><span style='color: red; font-weight: bold;'>*</span> กรุณา Login เข้าสู่ระบบ https://oneejobs27.oneeclick.co:7191/LoginAdmin และไปที่ Menu การว่าจ้าง เพื่อตอบกลับคำขอนี้ <span style='color: red; font-weight: bold;'>*</span></p>
                <br>
                <p style='color: red; font-weight: bold;'>**Email อัตโนมัติ โปรดอย่าตอบกลับ**</p>
            </div>";

            var recipients = await GetEmailRecipientsAsync(2);
            return await SendEmailsAsync(recipients, "ONEE Jobs - List of candidates for job interview", hrBody, null);
        }

        public async Task<int> SendManagerEmailsAsync(ApplicantRequestData requestData)
        {
            var candidateNames = requestData.Candidates?
                .Where(candidate =>
                    candidate.Status is "Success" or "Unsuccess" or "Cancel")
                .Select((candidate, index) =>
                {
                    string statusText = candidate.Status switch
                    {
                        "Success" => "สำเร็จ",
                        "Unsuccess" => "ต่อรองไม่สำเร็จ",
                        "Cancel" => "ยกเลิก",
                        _ => ""
                    };

                    string remarkText = (candidate.Status == "Unsuccess" && !string.IsNullOrWhiteSpace(candidate.Remark))
                        ? $" <span style='color: red;'>(หมายเหตุ: {candidate.Remark})</span>"
                        : "";

                    return $"ลำดับที่ {index + 1}: {candidate.Title} {candidate.FirstNameThai} {candidate.LastNameThai} สถานะ {statusText}{remarkText}".Trim();
                }).ToList() ?? [];

            string candidateNamesString = string.Join("<br>", candidateNames);
            // <p style='font-weight: bold; margin: 0 0 10px 0;'>เรียน คุณ {requestData.RequesterName}</p>
            // <p style='font-weight: bold; margin: 0 0 10px 0;'>เรียน Manager ฝ่าย {requestData.JobTitle} </p>
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

            var recipients = await GetEmailRecipientsAsync(3,requestData.Department);
            return await SendEmailsAsync(recipients, "ONEE Jobs - List of candidates for job interview", hrBody, null);
        }

        public async Task<int> SendHrEmailsAsync(ApplicantRequestData requestData)
        {
            var candidateNames = requestData.Candidates?
                .Select(candidate => $"{candidate.Title} {candidate.FirstNameThai} {candidate.LastNameThai}".Trim())
                .ToList() ?? [];

            string candidateNamesString = string.Join(" ", candidateNames);

            string hrBody = $@"
            <div style='font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px; font-size: 14px;'>
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
            return await SendEmailsAsync(recipients, "ONEE Jobs - List of candidates for job interview", hrBody, null);
        }

        public async Task<int> SendNotificationEmailsAsync(ApplicantRequestData requestData)
        {
            try
            {
                var candidateNames = requestData.Candidates?
                    .Select(candidate => $"{candidate.Title} {candidate.FirstNameThai} {candidate.LastNameThai}".Trim())
                    .ToList() ?? [];

                var candidateEmails = requestData.Candidates?
                    .Select(candidate => candidate.Email ?? "")
                    .ToList() ?? [];

                var jobIds = new List<int?> { requestData?.JobID };

                // var candidateApplicantIDs = requestData.Candidates?
                //     .Select(candidate => candidate.ApplicantID ?? "")
                //     .ToList() ?? [];
                var candidateApplicantIDs = requestData?.Candidates?
                .Select(candidate => candidate.ApplicantID)
                .ToList() ?? [];

                string candidateName = candidateNames.FirstOrDefault() ?? "ผู้สมัคร";
                // string candidateApplicantID = candidateApplicantIDs.FirstOrDefault() ?? "0";

                // string candidateApplicantID = candidateApplicantIDs.Count != 0
                // ? candidateApplicantIDs.First().ToString()
                // : "0";
                int candidateApplicantID = candidateApplicantIDs.Count != 0
                    ? candidateApplicantIDs.First()
                    : 0;
                int jobId = requestData?.JobID ?? 0; 

                using var connection = _context.CreateConnection();
                var url = new DynamicParameters();
                url.Add("@ApplicantID", candidateApplicantID, DbType.Int32);
                url.Add("@JobID", jobId, DbType.Int32);
                var urllist = await connection.QueryAsync<dynamic>(
                    "GetDataForEmailNotiSelectCandidate",
                    url,
                    commandType: CommandType.StoredProcedure);

                var urlResult = urllist.FirstOrDefault();
                string fromRegis = urlResult != null ? urlResult.LoginUrl?.ToString() ?? "ลิงก์ไม่พร้อมใช้งาน" : "ลิงก์ไม่พร้อมใช้งาน";

                string reqBody = $@"
                <div style='font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px; font-size: 14px;'>
                    <p style='font-weight: bold; margin: 0 0 10px 0;'>เรียน คุณ{candidateName}</p>
                    <p style='font-weight: bold; margin: 0 0 10px 0;'>เรื่อง: ผลสัมภาษณ์ผู้สมัครตำแหน่ง {requestData?.JobTitle}</p>
                    <br>
                    <p style='margin: 0 0 10px 0;'>
                        ตามที่ท่านได้สมัครในตำแหน่ง {requestData?.JobTitle} ทางบริษัทได้พิจารณาให้คุณผ่านการคัดเลือก กรุณาเข้าไปกรอกรายละเอียดของท่าน ตามลิงก์ด้านล่าง
                    </p>
                    <p style='margin: 0 0 10px 0;'>
                        Click : <a href='{fromRegis}'>{fromRegis}</a>
                    </p>
                    <br>
                    <p style='color: red; font-weight: bold;'>**อีเมลนี้เป็นข้อความอัตโนมัติ กรุณาอย่าตอบกลับ**</p>
                </div>";

                return await SendEmailsAsync(candidateEmails, "ONEE Jobs - List of selected candidates", reqBody, jobIds.FirstOrDefault());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing GetDataForEmailNotiSelectCandidate with");
                throw;
            }
        }

        private async Task<int> SendEmailsAsync(IEnumerable<string> recipients, string subject, string body, int? jobIds)
        {
            var recipientList = recipients
                .Where(email => !string.IsNullOrWhiteSpace(email))
                .ToList();

            if (recipientList.Count == 0) return 0;

            try
            {
                string to = string.Join(";", recipientList);

                await _emailService.SendEmailAsync(to, subject, body, true, "Register", jobIds);

                _logger.LogInformation("Successfully sent email to {Count} recipients", recipientList.Count);
                return recipientList.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to multiple recipients: {Message}", ex.Message);
                return 0;
            }
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

        public async Task<int> SendEmailsJobsStatusAsync(int JobID)
        {
            using var connection = _context.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("@JobID", JobID);
            var result = await connection.QueryAsync<dynamic>(
                "sp_GetDataSendMailJobs @JobID",
                parameters);
            var emails = result
                .Select(r => ((string?)r?.EMAIL)?.Trim())
                .Where(email => !string.IsNullOrWhiteSpace(email))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var firstRecord = result.FirstOrDefault();
            string hrBody = string.Empty;
            string SubjectMail = string.Empty;
            hrBody = $@"
            <div style='font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px; font-size: 14px; line-height: 1.6;'>
                <p style='margin: 0;'>เรียนคุณ {firstRecord?.NAMETHAI} และคุณ {firstRecord?.ApproveNameThai},</p>

                {(firstRecord?.ApprovalStatus == "Approved" ? $@"
                    <p>
                        ฝ่ายทรัพยากรบุคคลได้ดำเนินการ <strong>อนุมัติ</strong> คำขอเปิดรับสมัครงานในตำแหน่ง 
                        <strong>{firstRecord?.JobTitle}</strong> เรียบร้อยแล้วค่ะ
                    </p>
                " : $@"
                    <p>
                        ฝ่ายทรัพยากรบุคคลได้ดำเนินการ <strong>ไม่อนุมัติ</strong> คำขอเปิดรับสมัครงานในตำแหน่ง 
                        <strong>{firstRecord?.JobTitle}</strong> ด้วยเหตุผลดังต่อไปนี้ค่ะ:
                    </p>
                    <blockquote style='background-color:#fff3f3; padding: 10px; border-left: 4px solid #ff4d4f;'>
                        <strong>{firstRecord?.Remark}</strong>
                    </blockquote>
                    <p>หากต้องการข้อมูลเพิ่มเติม กรุณาติดต่อฝ่ายทรัพยากรบุคคลโดยตรงค่ะ</p>
                ")}

                <p style='margin-top: 30px;'>ด้วยความเคารพ,</p>
                <p style='margin: 0;'>ฝ่ายทรัพยากรบุคคล</p>
                <br>
                <p style='color:red; font-weight: bold;'>**อีเมลนี้คือข้อความอัตโนมัติ กรุณาอย่าตอบกลับ**</p>
            </div>";
            SubjectMail = $@"แจ้งสถานะคำขอเปิดรับสมัครพนักงาน - ตำแหน่ง {firstRecord?.JobTitle}";
            return await SendEmailsAsync(emails!, SubjectMail, hrBody, null);
        }

    }
}