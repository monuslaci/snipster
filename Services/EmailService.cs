using Microsoft.AspNetCore.Identity;
using MongoDB.Driver;
using static Snipster.Data.DBContext;
using BCrypt.Net;
using static Snipster.Data.CommonClasses;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Snipster.Services
{
    public class EmailService
    {
        private readonly ISendGridClient _sendGridClient;

        public EmailService(ISendGridClient sendGridClient)
        {
            _sendGridClient = sendGridClient;
        }

        public async Task SendForgotEmailNotification(string userEmail, string token)
        {
            var from = new EmailAddress("no-reply@yourapp.com", "Your App Name");
            var subject = "Reset Your Password";
            var to = new EmailAddress(userEmail);

            // Create the reset link
            var resetUrl = "";
            if (Environment.GetEnvironmentVariable("Environment") == "Development")
                resetUrl = $"https://localhost:7225/pw-reset?token={token}";
            else if (Environment.GetEnvironmentVariable("Environment") == "Production")
                resetUrl = $"https://yourapp.com/pw-reset?token={token}";

            var plainTextContent = $"Click the link to reset your password: {resetUrl}";
            var htmlContent = $"<p>Click <a href='{resetUrl}'>here</a> to reset your password.</p>";

            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
            await _sendGridClient.SendEmailAsync(msg);
        }

    }
}
