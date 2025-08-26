using Microsoft.Extensions.Options;
using JobOnlineAPI.Models;
using MailKit.Net.Smtp;
using MimeKit;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;

namespace JobOnlineAPI.Services
{
    public class EmailService(IConfiguration configuration, IOptions<EmailSettings> emailSettings) : IEmailService
    {
        private readonly EmailSettings _emailSettings = emailSettings.Value;
        private readonly IDbConnection _dbConnection = new SqlConnection(configuration.GetConnectionString("DefaultConnection"));

        // public async Task SendEmailAsync(string to, string subject, string body, bool isHtml, string typeMail)
        // {

        //     var emailMessage = new MimeMessage();
        //     emailMessage.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.FromEmail));
        //     emailMessage.To.Add(new MailboxAddress("", to));
        //     emailMessage.Subject = subject;

        //     var bodyBuilder = new BodyBuilder
        //     {
        //         HtmlBody = isHtml ? body : null,
        //         TextBody = !isHtml ? body : null
        //     };

        //     emailMessage.Body = bodyBuilder.ToMessageBody();
        //     try
        //     {

        //         using var client = new SmtpClient();
        //         await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.SmtpPort, _emailSettings.UseSSL);
        //         if (!string.IsNullOrEmpty(_emailSettings.SmtpUser) && !string.IsNullOrEmpty(_emailSettings.SmtpPass))
        //         {
        //             await client.AuthenticateAsync(_emailSettings.SmtpUser, _emailSettings.SmtpPass);
        //         }
        //         await client.SendAsync(emailMessage);
        //         await client.DisconnectAsync(true);
        //     }
        //     catch (Exception ex)
        //     {
        //         Console.WriteLine($"Error sending email: {ex.Message}");
        //         throw;
        //     }

        // }

        // public async Task SendEmailAsync(string to, string subject, string body, bool isHtml, string typeMail, int? JobsId)
        // {
        //     var emailMessage = new MimeMessage();
        //     emailMessage.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.FromEmail));
        //     emailMessage.To.Add(new MailboxAddress("", to));
        //     emailMessage.Subject = subject;

        //     var bodyBuilder = new BodyBuilder
        //     {
        //         HtmlBody = isHtml ? body : null,
        //         TextBody = !isHtml ? body : null
        //     };

        //     emailMessage.Body = bodyBuilder.ToMessageBody();

        //     string status = "Success";
        //     string? errorMessage = null;

        //     try
        //     {
        //         using var client = new SmtpClient();
        //         await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.SmtpPort, _emailSettings.UseSSL);

        //         if (!string.IsNullOrEmpty(_emailSettings.SmtpUser) && !string.IsNullOrEmpty(_emailSettings.SmtpPass))
        //         {
        //             await client.AuthenticateAsync(_emailSettings.SmtpUser, _emailSettings.SmtpPass);
        //         }

        //         await client.SendAsync(emailMessage);
        //         await client.DisconnectAsync(true);
        //     }
        //     catch (Exception ex)
        //     {
        //         status = "Failed";
        //         errorMessage = ex.ToString(); // หรือแค่ ex.Message ก็ได้
        //         Console.WriteLine($"❌ Error sending email: {errorMessage}");
        //     }

        //     await LogEmailAsync(to, subject, body, status, errorMessage, typeMail, JobsId);
        // }

        public async Task SendEmailAsync(string to, string subject, string body, bool isHtml, string typeMail, int? JobsId)
        {
            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.FromEmail));
            
            // ✅ รองรับหลาย recipients
            foreach (var address in to.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                emailMessage.To.Add(new MailboxAddress("", address.Trim()));
            }

            emailMessage.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = isHtml ? body : null,
                TextBody = !isHtml ? body : null
            };

            emailMessage.Body = bodyBuilder.ToMessageBody();

            string status = "Success";
            string? errorMessage = null;

            try
            {
                using var client = new SmtpClient();
                await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.SmtpPort, _emailSettings.UseSSL);

                if (!string.IsNullOrEmpty(_emailSettings.SmtpUser) && !string.IsNullOrEmpty(_emailSettings.SmtpPass))
                {
                    await client.AuthenticateAsync(_emailSettings.SmtpUser, _emailSettings.SmtpPass);
                }

                await client.SendAsync(emailMessage);
                await client.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                status = "Failed";
                errorMessage = ex.Message;
                Console.WriteLine($"❌ Error sending email: {errorMessage}");
            }

            await LogEmailAsync(to, subject, body, status, errorMessage, typeMail, JobsId);
        }

        private async Task LogEmailAsync(string recipient, string subject, string body, string status, string? errorMessage, string? mailType, int? JobsId)
        {
            using var connection = new SqlConnection(_dbConnection.ConnectionString);
            var parameters = new
            {
                Recipient = recipient,
                Subject = subject,
                Body = body,
                Status = status,
                ErrorMessage = errorMessage,
                MailType = mailType,
                SystemTag = "ONEEJobs",
                JobID = JobsId
            };

            await connection.ExecuteAsync("sp_LogEmailActivity", parameters, commandType: CommandType.StoredProcedure);
        }
    }
}