// using System.Net.Mail;
using System.Net;
using Microsoft.Extensions.Options;
using JobOnlineAPI.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace JobOnlineAPI.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;

        public EmailService(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }

        public async Task SendEmailAsync(string to, string subject, string body, bool isHtml)
        {

         var emailMessage = new MimeMessage();
         emailMessage.To.Add(new MailboxAddress("", to));
         emailMessage.Subject = subject;

         var bodyBuilder = new BodyBuilder
         {
             HtmlBody = isHtml ? body : null,
             TextBody = !isHtml ? body : null
         };

         emailMessage.Body = bodyBuilder.ToMessageBody();
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
             Console.WriteLine($"Error sending email: {ex.Message}");
             throw;
         }


            // try {
            //         using var smtpClient = new SmtpClient(_emailSettings.SmtpServer)
            //         {
            //             Port = _emailSettings.SmtpPort,
            //             Credentials = new NetworkCredential(_emailSettings.SmtpUser, _emailSettings.SmtpPass),
            //             EnableSsl = true
            //         };

            //         var mailMessage = new MailMessage
            //         {
            //             From = new MailAddress(_emailSettings.FromEmail),
            //             Subject = subject,
            //             Body = body,
            //             IsBodyHtml = isHtml
            //         };

            //         mailMessage.To.Add(to);

            //         await smtpClient.SendMailAsync(mailMessage);
            // } catch (Exception ex) {
            //     Console.WriteLine($"❌ Error sending email to {to}: {ex.Message}");
            //     throw;
            // }
       
        }
    }
}