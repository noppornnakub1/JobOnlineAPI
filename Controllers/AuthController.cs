using Microsoft.AspNetCore.Mvc;
using JobOnlineAPI.DAL;
using JobOnlineAPI.Services;
using Dapper;
using System.Security.Cryptography;
using System.Net.Mail;

namespace JobOnlineAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(DapperContext context, IEmailService emailService, ILogger<AuthController> logger) : ControllerBase
    {
        private readonly DapperContext _context = context ?? throw new ArgumentNullException(nameof(context));
        private readonly IEmailService _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        private readonly ILogger<AuthController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // POST: api/auth/request-otp
        [HttpPost("request-otp")]
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
                // สร้าง OTP ด้วย RandomNumberGenerator
                string otp = GetGenerateOTP();

                using var connection = _context.CreateConnection();
                var parameters = new DynamicParameters();
                parameters.Add("@Email", request.Email);
                parameters.Add("@Action", request.Action);
                parameters.Add("@OTP", otp); // ส่ง OTP จาก Backend
                parameters.Add("@ErrorMessage", dbType: System.Data.DbType.String, direction: System.Data.ParameterDirection.Output, size: 500);

                await connection.ExecuteAsync("[dbo].[usp_GenerateOTP]",
                    parameters,
                    commandType: System.Data.CommandType.StoredProcedure);

                string errorMessage = parameters.Get<string>("@ErrorMessage");

                if (!string.IsNullOrEmpty(errorMessage))
                {
                    _logger.LogWarning("RequestOTP: ข้อผิดพลาดจาก usp_GenerateOTP สำหรับ Email: {Email}, Error: {ErrorMessage}", request.Email, errorMessage);
                    return BadRequest(new { Error = errorMessage });
                }

                // ส่งอีเมล OTP ด้วย IEmailService
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
            catch (SmtpException ex)
            {
                _logger.LogError(ex, "RequestOTP: ไม่สามารถส่งอีเมลไปยัง {Email}: {Message}", request.Email, ex.Message);
                return StatusCode(500, new { Error = "ไม่สามารถส่งอีเมล OTP ได้" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RequestOTP: เกิดข้อผิดพลาดสำหรับ Email: {Email}: {Message}", request.Email, ex.Message);
                return StatusCode(500, new { Error = "เกิดข้อผิดพลาดในระบบ: " + ex.Message });
            }
        }

        // POST: api/auth/verify-otp
        [HttpPost("verify-otp")]
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

        // POST: api/auth/register
        [HttpPost("register")]
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
                // เข้ารหัสรหัสผ่านด้วย BCrypt
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

                using var connection = _context.CreateConnection();
                var parameters = new DynamicParameters();
                parameters.Add("@Email", request.Email);
                parameters.Add("@PasswordHash", passwordHash);
                parameters.Add("@ConfirmConsent", request.ConfirmConsent ?? "Yes");
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

                // ส่งอีเมลต้อนรับ (ถ้าต้องการ)
                string welcomeBody = $@"<div style='font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px; font-size: 14px;'>
                                        <p style='font-weight: bold; margin: 0 0 10px 0;'>เรียน ผู้ใช้</p>
                                        <p style='margin: 0 0 10px 0;'>ยินดีต้อนรับสู่ ONEE Jobs!</p>
                                        <p style='margin: 0 0 10px 0;'>การสมัครสมาชิกของคุณด้วยอีเมล {request.Email} สำเร็จแล้ว</p>
                                        <p style='color: red; font-weight: bold;'>**อีเมลนี้เป็นข้อความอัตโนมัติ กรุณาอย่าตอบกลับ**</p>
                                     </div>";

                await _emailService.SendEmailAsync(request.Email, "ยินดีต้อนรับสู่ ONEE Jobs", welcomeBody, true);

                return Ok(new { Message = "สมัครสมาชิกสำเร็จ" });
            }
            catch (SmtpException ex)
            {
                _logger.LogError(ex, "Register: ไม่สามารถส่งอีเมลต้อนรับไปยัง {Email}: {Message}", request.Email, ex.Message);
                return Ok(new { Message = "สมัครสมาชิกสำเร็จ แต่ไม่สามารถส่งอีเมลต้อนรับได้" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Register: เกิดข้อผิดพลาดสำหรับ Email: {Email}: {Message}", request.Email, ex.Message);
                return StatusCode(500, new { Error = "เกิดข้อผิดพลาดในระบบ: " + ex.Message });
            }
        }

        // POST: api/auth/reset-password
        [HttpPost("reset-password")]
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
                // เข้ารหัสรหัสผ่านด้วย BCrypt
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

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

                // ส่งอีเมลยืนยันการรีเซ็ต (ถ้าต้องการ)
                string resetBody = $@"<div style='font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px; font-size: 14px;'>
                                       <p style='font-weight: bold; margin: 0 0 10px 0;'>เรียน ผู้ใช้</p>
                                       <p style='margin: 0 0 10px 0;'>การรีเซ็ตรหัสผ่านสำหรับอีเมล {request.Email} สำเร็จแล้ว</p>
                                       <p style='margin: 0 0 10px 0;'>กรุณาใช้รหัสผ่านใหม่เพื่อเข้าสู่ระบบ</p>
                                       <p style='color: red; font-weight: bold;'>**อีเมลนี้เป็นข้อความอัตโนมัติ กรุณาอย่าตอบกลับ**</p>
                                    </div>";

                await _emailService.SendEmailAsync(request.Email, "รีเซ็ตรหัสผ่านสำเร็จ", resetBody, true);

                return Ok(new { Message = "รีเซ็ตรหัสผ่านสำเร็จ" });
            }
            catch (SmtpException ex)
            {
                _logger.LogError(ex, "ResetPassword: ไม่สามารถส่งอีเมลยืนยันไปยัง {Email}: {Message}", request.Email, ex.Message);
                return Ok(new { Message = "รีเซ็ตรหัสผ่านสำเร็จ แต่ไม่สามารถส่งอีเมลยืนยันได้" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ResetPassword: เกิดข้อผิดพลาดสำหรับ Email: {Email}: {Message}", request.Email, ex.Message);
                return StatusCode(500, new { Error = "เกิดข้อผิดพลาดในระบบ: " + ex.Message });
            }
        }

        // ฟังก์ชันสร้าง OTP ด้วย RandomNumberGenerator
        private static string GetGenerateOTP()
        {
            byte[] randomBytes = new byte[4];
            RandomNumberGenerator.Fill(randomBytes);
            int otpValue = Math.Abs(BitConverter.ToInt32(randomBytes, 0) % 1000000);
            return otpValue.ToString("D6"); // สร้าง OTP 6 หลัก
        }

        // ฟังก์ชันช่วยตรวจสอบรูปแบบอีเมล
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

    // คลาสสำหรับรับข้อมูลจาก request
    public class OTPRequest
    {
        public required string Email { get; set; }
        public required string Action { get; set; } // REGISTER หรือ RESET
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
        public required string ConfirmConsent { get; set; } // Yes หรือ No
    }

    public class ResetPasswordRequest
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
    }
}