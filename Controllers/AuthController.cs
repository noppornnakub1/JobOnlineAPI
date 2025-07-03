using Microsoft.AspNetCore.Mvc;
using JobOnlineAPI.DAL;
using JobOnlineAPI.Services;
using Dapper;
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

                string subject = request.Action == "REGISTER" ? "รหัส OTP สำหรับสมัครสมาชิก" : "รหัส OTP สำหรับรีเซ็ตรหัสผ่าน";
                string body = $@"<div style='font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px; font-size: 14px;'>
                                    <p style='font-weight: bold; margin: 0 0 10px 0;'>เรียน ผู้ใช้</p>
                                    <p style='margin: 0 0 10px 0;'>รหัส OTP ของคุณคือ: <strong>{otp}</strong></p>
                                    <p style='margin: 0 0 10px 0;'>รหัสนี้ใช้ได้ภายใน 10 นาที กรุณานำไปใช้เพื่อยืนยันตัวตน</p>
                                    <p style='color: red; font-weight: bold;'>**อีเมลนี้เป็นข้อความอัตโนมัติ กรุณาอย่าตอบกลับ**</p>
                                 </div>";

                await _emailService.SendEmailAsync(request.Email, subject, body, true);

                _logger.LogInformation("RequestOTP: ส่ง OTP สำเร็จสำหรับ Email: {Email}, Action: {Action}", request.Email, request.Action);
                return Ok(new { Message = "ส่ง OTP ไปยังอีเมลเรียบร้อยแล้ว" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RequestOTP: เกิดข้อผิดพลาดสำหรับ Email: {Email}: {Message}", request.Email, ex.Message);
                return StatusCode(500, new { Error = "เกิดข้อผิดพลาดในระบบ: " + ex.Message });
            }
        }

        /// <summary>
        /// ยืนยัน OTP ที่ผู้ใช้ป้อน
        /// </summary>
        /// <param name="request">ข้อมูลอีเมลและ OTP</param>
        /// <returns>สถานะการยืนยัน OTP</returns>
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
                return Ok(new { IsValid = isValid, Message = isValid ? "ยืนยัน OTP สำเร็จ" : "OTP ไม่ถูกต้อง" });
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

                string welcomeBody = $@"<div style='font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px; font-size: 14px;'>
                                        <p style='font-weight: bold; margin: 0 0 10px 0;'>เรียน ผู้ใช้</p>
                                        <p style='margin: 0 0 10px 0;'>ยินดีต้อนรับสู่ ONEE Jobs!</p>
                                        <p style='margin: 0 0 10px 0;'>การสมัครสมาชิกของคุณด้วยอีเมล {request.Email} สำเร็จแล้ว</p>
                                        <p style='color: red; font-weight: bold;'>**อีเมลนี้เป็นข้อความอัตโนมัติ กรุณาอย่าตอบกลับ**</p>
                                     </div>";

                await _emailService.SendEmailAsync(request.Email, "ยินดีต้อนรับสู่ ONEE Jobs", welcomeBody, true);

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

                string resetBody = $@"<div style='font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px; font-size: 14px;'>
                                       <p style='font-weight: bold; margin: 0 0 10px 0;'>เรียน ผู้ใช้</p>
                                       <p style='margin: 0 0 10px 0;'>การรีเซ็ตรหัสผ่านสำหรับอีเมล {request.Email} สำเร็จแล้ว</p>
                                       <p style='margin: 0 0 10px 0;'>กรุณาใช้รหัสผ่านใหม่เพื่อเข้าสู่ระบบ</p>
                                       <p style='color: red; font-weight: bold;'>**อีเมลนี้เป็นข้อความอัตโนมัติ กรุณาอย่าตอบกลับ**</p>
                                    </div>";

                await _emailService.SendEmailAsync(request.Email, "รีเซ็ตรหัสผ่านสำเร็จ", resetBody, true);

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