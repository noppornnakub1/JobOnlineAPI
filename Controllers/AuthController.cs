using Dapper;
using JobOnlineAPI.DAL;
using JobOnlineAPI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;

namespace JobOnlineAPI.Controllers
{
    /// <summary>
    /// API สำหรับการจัดการการสมัครสมาชิกและรีเซ็ตรหัสผ่านด้วย OTP
    /// </summary>
    /// <remarks>
    /// Constructor: รับ DapperContext, IEmailService, และ ILogger
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(DapperContext context, IEmailService emailService, ILogger<AuthController> logger) : ControllerBase
    {
        private readonly DapperContext _context = context ?? throw new ArgumentNullException(nameof(context));
        private readonly IEmailService _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        private readonly ILogger<AuthController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly string _templatePathOTP = Path.Combine("Templates", "Email", "OTP.html");
        private readonly string _templatePathREGIS = Path.Combine("Templates", "Email", "regis.html");
        //private readonly string _templatePathOTP = Path.Combine("Templates", "Email", "RequestOtp.html");
        private readonly TimeSpan _tokenExpiration = TimeSpan.FromMinutes(10); // โทเคนหมดอายุใน 10 นาที

        // เก็บโทเคนชั่วคราว (ควรใช้ฐานข้อมูลในโปรดักชัน)
        private static readonly Dictionary<string, (string Otp, DateTime Expires)> _tokenStore = [];

        /// <summary>
        /// ขอ OTP สำหรับสมัครสมาชิกหรือรีเซ็ตรหัสผ่าน
        /// </summary>
        /// <param name="request">ข้อมูลอีเมลและประเภทการดำเนินการ (REGISTER หรือ RESET)</param>
        /// <returns>สถานะการส่ง OTP</returns>
        /// <response code="200">ส่ง OTP ไปยังอีเมลเรียบร้อย</response>
        /// <response code="400">ข้อมูลไม่ถูกต้องหรืออีเมลซ้ำ</response>
        /// <response code="500">เกิดข้อผิดพลาดในระบบ</response>
        [HttpPost("request-otp")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RequestOTP([FromBody] OTPRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Action))
            {
                _logger.LogWarning("RequestOTP: Email หรือ Action ว่างเปล่า");
                return BadRequest(new { Error = "ต้องระบุ Email และ Action" });
            }

            if (!IsValidEmail(request.Email))
            {
                _logger.LogWarning("RequestOTP: รูปแบบอีเมลไม่ถูกต้อง: {Email}", request.Email);
                return BadRequest(new { Error = "รูปแบบอีเมลไม่ถูกต้อง" });
            }

            if (!request.Action.Equals("REGISTER", StringComparison.OrdinalIgnoreCase) &&
                !request.Action.Equals("RESET", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("RequestOTP: Action ไม่ถูกต้อง: {Action}", request.Action);
                return BadRequest(new { Error = "Action ต้องเป็น REGISTER หรือ RESET" });
            }

            try
            {
                using var connection = _context.CreateConnection();
                var parameters = new DynamicParameters();
                parameters.Add("@Email", request.Email);
                parameters.Add("@Action", request.Action);
                parameters.Add("@OTP", dbType: System.Data.DbType.String, direction: System.Data.ParameterDirection.Output, size: 6);
                parameters.Add("@ErrorMessage", dbType: System.Data.DbType.String, direction: System.Data.ParameterDirection.Output, size: 500);

                await connection.ExecuteAsync("[dbo].[usp_GenerateOTP]",
                    parameters,
                    commandType: System.Data.CommandType.StoredProcedure);

                string otp = parameters.Get<string>("@OTP");
                string errorMessage = parameters.Get<string>("@ErrorMessage");

                if (!string.IsNullOrEmpty(errorMessage))
                {
                    _logger.LogWarning("RequestOTP: ข้อผิดพลาดจาก usp_GenerateOTP สำหรับ Email: {Email}, Error: {ErrorMessage}", request.Email, errorMessage);
                    return BadRequest(new { Error = errorMessage });
                }

                string subject = request.Action == "REGISTER"
                    ? "✅ ONEE Jobs: รหัส OTP สำหรับการสมัครสมาชิก"
                    : "🔒 ONEE Jobs: รหัส OTP สำหรับรีเซ็ตรหัสผ่าน";
                string username = request.Email.Split('@').FirstOrDefault() ?? "ผู้ใช้";
                string actionDescription = request.Action.Equals("register", StringComparison.CurrentCultureIgnoreCase) ? "การสมัครสมาชิก" : "การรีเซ็ตรหัสผ่าน";

                // สร้างโทเคนและ URL สำหรับคัดลอก
                string token = Guid.NewGuid().ToString();
                _tokenStore[token] = (otp, DateTime.UtcNow + _tokenExpiration);
                string copyUrl = Url.Action("CopyOtp", "Auth", new { otp, token }, Request.Scheme) ?? "#";

                // โหลดและเติมข้อมูลในเทมเพลต
                string template = System.IO.File.ReadAllText(_templatePathOTP);
                string body = template
                    .Replace("{{otp}}", otp)
                    .Replace("{{copyUrl}}", copyUrl);

                await _emailService.SendEmailAsync(request.Email, subject, body, true);

                _logger.LogInformation("RequestOTP: ส่ง OTP สำเร็จสำหรับ Email: {Email}, Action: {Action}", request.Email, request.Action);
                return Ok(new { Message = "ส่ง OTP ไปยังอีเมลเรียบร้อยแล้ว" });
            }
            catch (Exception ex) when (ex is FileNotFoundException)
            {
                _logger.LogError(ex, "RequestOTP: ไม่พบไฟล์เทมเพลต: {Path}", _templatePathOTP);
                return StatusCode(500, new { Error = "เกิดข้อผิดพลาดในระบบ: ไฟล์เทมเพลตไม่พบ" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RequestOTP: เกิดข้อผิดพลาดสำหรับ Email: {Email}: {Message}", request.Email, ex.Message);
                return StatusCode(500, new { Error = "เกิดข้อผิดพลาดในระบบ: " + ex.Message });
            }
        }

        /// <summary>
        /// หน้าเว็บสำหรับคัดลอก OTP และปิดแท็บทันที
        /// </summary>
        /// <param name="otp">รหัส OTP ที่จะคัดลอก</param>
        /// <param name="token">โทเคนเพื่อยืนยันความถูกต้อง</param>
        /// <returns>หน้า HTML ด้วย JavaScript เพื่อคัดลอก OTP</returns>
        [HttpGet("copy-otp")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public IActionResult CopyOtp(string otp, string token)
        {
            if (string.IsNullOrEmpty(otp) || string.IsNullOrEmpty(token) ||
                !_tokenStore.TryGetValue(token, out var tokenData) || tokenData.Otp != otp || tokenData.Expires < DateTime.UtcNow)
            {
                _logger.LogWarning("CopyOtp: โทเคนไม่ถูกต้องหรือหมดอายุสำหรับ OTP: {Otp}", otp);
                return Unauthorized("โทเคนไม่ถูกต้องหรือหมดอายุ");
            }

            return Content($@"
                <!DOCTYPE html>
                <html lang='th'>
                <head>
                    <meta charset='UTF-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <title>คัดลอก OTP</title>
                    <script src=""https://cdn.tailwindcss.com""></script>
                </head>
                <body class=""bg-gray-100 flex items-center justify-center min-h-screen"">
                    <div id=""successCard"" class=""bg-white rounded-2xl shadow-2xl p-8 max-w-md w-full text-center transform transition-all duration-300 scale-100"">
                        <div class=""mb-6"">
                            <svg class=""w-16 h-16 mx-auto text-green-500"" fill=""none"" stroke=""currentColor"" viewBox=""0 0 24 24"" xmlns=""http://www.w3.org/2000/svg"">
                                <path stroke-linecap=""round"" stroke-linejoin=""round"" stroke-width=""2"" d=""M5 13l4 4L19 7""></path>
                            </svg>
                        </div>
                        <h2 class=""text-2xl font-bold text-gray-800 mb-4"">คัดลอก OTP สำเร็จ!</h2>
                        <p class=""text-gray-600 mb-6"">OTP <span class=""font-mono font-semibold"">{otp}</span> ถูกคัดลอกไปยังคลิปบอร์ดแล้ว</p>
                        <button id=""okButton"" class=""bg-blue-600 text-white font-semibold py-3 px-6 rounded-lg hover:bg-blue-700 transition-colors duration-200 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2"">
                            ตกลง
                        </button>
                    </div>
                    <div id=""errorCard"" class=""hidden bg-white rounded-2xl shadow-2xl p-8 max-w-md w-full text-center transform transition-all duration-300 scale-100"">
                        <div class=""mb-6"">
                            <svg class=""w-16 h-16 mx-auto text-red-500"" fill=""none"" stroke=""currentColor"" viewBox=""0 0 24 24"" xmlns=""http://www.w3.org/2000/svg"">
                                <path stroke-linecap=""round"" stroke-linejoin=""round"" stroke-width=""2"" d=""M6 18L18 6M6 6l12 12""></path>
                            </svg>
                        </div>
                        <h2 class=""text-2xl font-bold text-gray-800 mb-4"">ไม่สามารถคัดลอก OTP ได้</h2>
                        <p class=""text-gray-600 mb-6"">กรุณาคัดลอก OTP ด้วยตนเอง: <span class=""font-mono font-semibold"">{{otp}}</span></p>
                        <button id=""errorOkButton"" class=""bg-blue-600 text-white font-semibold py-3 px-6 rounded-lg hover:bg-blue-700 transition-colors duration-200 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2"">
                            ตกลง
                        </button>
                    </div>
                    <script>
                        function copyAndClose() {{
                            navigator.clipboard.writeText('{otp}').then(() => {{
                                document.getElementById('successCard').classList.remove('scale-0');
                                document.getElementById('successCard').classList.add('scale-100');
                            }}).catch(err => {{
                                document.getElementById('successCard').classList.add('hidden');
                                document.getElementById('errorCard').classList.remove('hidden');
                                document.getElementById('errorCard').classList.remove('scale-0');
                                document.getElementById('errorCard').classList.add('scale-100');
                            }});
                        }}

                        document.getElementById('okButton').addEventListener('click', () => {{
                            window.close();
                        }});

                        document.getElementById('errorOkButton').addEventListener('click', () => {{
                            window.close();
                        }});

                        window.onload = copyAndClose;
                    </script>
                </body>
                </html>", "text/html");
        }

        /// <summary>
        /// ยืนยัน OTP ที่ผู้ใช้ป้อน
        /// </summary>
        /// <param name="request">ข้อมูลอีเมลและ OTP</param>
        /// <returns>สถานะการยัน OTP</returns>
        /// <response code="200">ยืนยัน OTP สำเร็จหรือไม่สำเร็จ</response>
        /// <response code="400">ข้อมูลไม่ถูกต้องหรือ OTP ไม่ถูกต้อง</response>
        /// <response code="500">เกิดข้อผิดพลาดในระบบ</response>
        [HttpPost("verify-otp")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> VerifyOTP([FromBody] OTPVerifyRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.OTP))
            {
                _logger.LogWarning("VerifyOTP: Email หรือ OTP ว่างเปล่า");
                return BadRequest(new { Error = "ต้องระบุ Email และ OTP" });
            }

            try
            {
                using var connection = _context.CreateConnection();
                var parameters = new DynamicParameters();
                parameters.Add("@Email", request.Email);
                parameters.Add("@OTP", request.OTP);
                parameters.Add("@IsValid", dbType: System.Data.DbType.Boolean, direction: System.Data.ParameterDirection.Output);
                parameters.Add("@ErrorMessage", dbType: System.Data.DbType.String, direction: System.Data.ParameterDirection.Output, size: 500);

                await connection.ExecuteAsync("[dbo].[usp_VerifyOTP]",
                    parameters,
                    commandType: System.Data.CommandType.StoredProcedure);

                bool isValid = parameters.Get<bool>("@IsValid");
                string errorMessage = parameters.Get<string>("@ErrorMessage");

                if (!string.IsNullOrEmpty(errorMessage))
                {
                    _logger.LogWarning("VerifyOTP: ข้อผิดพลาดจาก usp_VerifyOTP สำหรับ Email: {Email}, OTP: {OTP}, Error: {ErrorMessage}",
                        request.Email, request.OTP, errorMessage);
                    return BadRequest(new { Error = errorMessage });
                }

                _logger.LogInformation("VerifyOTP: ยืนยัน OTP สำเร็จสำหรับ Email: {Email}, IsValid: {IsValid}", request.Email, isValid);
                return Ok(new { IsValid = isValid, Message = isValid ? "ยืนยัน OTP สำเร็จ" : "OTP ไม่ถูกต้องหรือหมดอายุ" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "VerifyOTP: เกิดข้อผิดพลาดสำหรับ Email: {Email}: {Message}", request.Email, ex.Message);
                return StatusCode(500, new { Error = "เกิดข้อผิดพลาดในระบบ: " + ex.Message });
            }
        }

        /// <summary>
        /// สมัครสมาชิกผู้ใช้ใหม่ (ต้องยืนยัน OTP ก่อน)
        /// </summary>
        /// <param name="request">ข้อมูลอีเมลและรหัสผ่าน</param>
        /// <returns>สถานะการสมัครสมาชิก</returns>
        /// <response code="200">สมัครสมาชิกสำเร็จ</response>
        /// <response code="400">ข้อมูลไม่ถูกต้อง, อีเมลซ้ำ, หรือ OTP ไม่ได้รับการยืนยัน</response>
        /// <response code="500">เกิดข้อผิดพลาดในระบบ</response>
        [HttpPost("register")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                _logger.LogWarning("Register: Email หรือ Password ว่างเปล่า");
                return BadRequest(new { Error = "ต้องระบุ Email และ Password" });
            }

            if (!IsValidEmail(request.Email))
            {
                _logger.LogWarning("Register: รูปแบบอีเมลไม่ถูกต้อง: {Email}", request.Email);
                return BadRequest(new { Error = "รูปแบบอีเมลไม่ถูกต้อง" });
            }

            try
            {
                string passwordHash = HashPassword(request.Password);

                using var connection = _context.CreateConnection();
                var parameters = new DynamicParameters();
                parameters.Add("@Email", request.Email);
                parameters.Add("@PasswordHash", passwordHash);
                parameters.Add("@ErrorMessage", dbType: System.Data.DbType.String, direction: System.Data.ParameterDirection.Output, size: 500);

                await connection.ExecuteAsync("[dbo].[usp_RegisterUser]",
                    parameters,
                    commandType: System.Data.CommandType.StoredProcedure);

                string errorMessage = parameters.Get<string>("@ErrorMessage");

                if (!string.IsNullOrEmpty(errorMessage))
                {
                    _logger.LogWarning("Register: ข้อผิดพลาดจาก usp_RegisterUser สำหรับ Email: {Email}, Error: {ErrorMessage}",
                        request.Email, errorMessage);
                    return BadRequest(new { Error = errorMessage });
                }

                _logger.LogInformation("Register: สมัครสมาชิกสำเร็จสำหรับ Email: {Email}", request.Email);

                string subject = "🎉 ยินดีต้อนรับสู่ ONEE Jobs";

                // โหลดและเติมข้อมูลในเทมเพลต
                string template = System.IO.File.ReadAllText(_templatePathREGIS);
                string body = template;

                await _emailService.SendEmailAsync(request.Email, subject, body, true);

                return Ok(new { Message = "สมัครสมาชิกสำเร็จ" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Register: เกิดข้อผิดพลาดสำหรับ Email: {Email}: {Message}", request.Email, ex.Message);
                return StatusCode(500, new { Error = "เกิดข้อผิดพลาดในระบบ: " + ex.Message });
            }
        }

        /// <summary>
        /// รีเซ็ตรหัสผ่านผู้ใช้ (ต้องยืนยัน OTP ก่อน)
        /// </summary>
        /// <param name="request">ข้อมูลอีเมลและรหัสผ่านใหม่</param>
        /// <returns>สถานะการรีเซ็ตรหัสผ่าน</returns>
        /// <response code="200">รีเซ็ตรหัสผ่านสำเร็จ</response>
        /// <response code="400">ข้อมูลไม่ถูกต้อง, อีเมลไม่พบ, หรือ OTP ไม่ได้รับการยืนยัน</response>
        /// <response code="500">เกิดข้อผิดพลาดในระบบ</response>
        [HttpPost("reset-password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                _logger.LogWarning("ResetPassword: Email หรือ Password ว่างเปล่า");
                return BadRequest(new { Error = "ต้องระบุ Email และ Password" });
            }

            if (!IsValidEmail(request.Email))
            {
                _logger.LogWarning("ResetPassword: รูปแบบอีเมลไม่ถูกต้อง: {Email}", request.Email);
                return BadRequest(new { Error = "รูปแบบอีเมลไม่ถูกต้อง" });
            }

            try
            {
                string passwordHash = HashPassword(request.Password);

                using var connection = _context.CreateConnection();
                var parameters = new DynamicParameters();
                parameters.Add("@Email", request.Email);
                parameters.Add("@PasswordHash", passwordHash);
                parameters.Add("@ErrorMessage", dbType: System.Data.DbType.String, direction: System.Data.ParameterDirection.Output, size: 500);

                await connection.ExecuteAsync("[dbo].[usp_UpdatePassword]",
                    parameters,
                    commandType: System.Data.CommandType.StoredProcedure);

                string errorMessage = parameters.Get<string>("@ErrorMessage");

                if (!string.IsNullOrEmpty(errorMessage))
                {
                    _logger.LogWarning("ResetPassword: ข้อผิดพลาดจาก usp_UpdatePassword สำหรับ Email: {Email}, Error: {ErrorMessage}",
                        request.Email, errorMessage);
                    return BadRequest(new { Error = errorMessage });
                }

                _logger.LogInformation("ResetPassword: รีเซ็ตรหัสผ่านสำเร็จสำหรับ Email: {Email}", request.Email);

                string subject = "🔑 ONEE Jobs: รีเซ็ตรหัสผ่านสำเร็จ";
                string body = $@"
                    <!DOCTYPE html>
                    <html lang='th'>
                    <head>
                        <meta charset='UTF-8'>
                        <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                        <style>
                            body {{ font-family: 'Arial', sans-serif; background-color: #f4f4f4; margin: 0; padding: 0; }}
                            .container {{ max-width: 600px; margin: 20px auto; background-color: #ffffff; border-radius: 10px; overflow: hidden; box-shadow: 0 0 10px rgba(0,0,0,0.1); }}
                            .header {{ background-color: #1a73e8; color: white; text-align: center; padding: 20px; }}
                            .header h1 {{ margin: 0; font-size: 24px; }}
                            .content {{ padding: 20px; color: #333; }}
                            .content p {{ margin: 0 0 15px; line-height: 1.6; }}
                            .footer {{ text-align: center; padding: 10px; color: #777; font-size: 12px; background-color: #f8f9fa; }}
                            .footer a {{ color: #1a73e8; text-decoration: none; }}
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <div class='header'>
                                <h1>ONEE Jobs</h1>
                            </div>
                            <div class='content'>
                                <p>เรียน คุณ {request.Email.Split('@')[0]},</p>
                                <p>การรีเซ็ตรหัสผ่านสำหรับบัญชีอีเมล {request.Email} ของคุณสำเร็จแล้ว</p>
                                <p>กรุณาใช้รหัสผ่านใหม่เพื่อเข้าสู่ระบบทันที หากคุณไม่ได้ร้องขอการเปลี่ยนแปลงนี้ กรุณาติดต่อทีมสนับสนุนทันที</p>
                            </div>
                            <div class='footer'>
                                <p>© 2025 ONEE Jobs | <a href='mailto:support@oneejobs.com'>support@oneejobs.com</a></p>
                                <p>ข้อความนี้เป็นการแจ้งอัตโนมัติ กรุณาอย่าตอบกลับ</p>
                            </div>
                        </div>
                    </body>
                    </html>";

                await _emailService.SendEmailAsync(request.Email, subject, body, true);

                return Ok(new { Message = "รีเซ็ตรหัสผ่านสำเร็จ" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ResetPassword: เกิดข้อผิดพลาดสำหรับ Email: {Email}: {Message}", request.Email, ex.Message);
                return StatusCode(500, new { Error = "เกิดข้อผิดพลาดในระบบ: " + ex.Message });
            }
        }

        private static string HashPassword(string password)
        {
            var hashedBytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
        }

        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email && email.Contains('@') && email.Contains('.');
            }
            catch
            {
                return false;
            }
        }
    }

    public class OTPRequest
    {
        public required string Email { get; set; }
        public required string Action { get; set; }
    }

    public class OTPVerifyRequest
    {
        public required string Email { get; set; }
        public required string OTP { get; set; }
    }

    public class RegisterRequest
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
    }

    public class ResetPasswordRequest
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
    }
}