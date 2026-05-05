using Microsoft.AspNetCore.Identity;
using MongoDB.Driver;
using static Snipster.Data.DBContext;
using BCrypt.Net;
using static Snipster.Data.CommonClasses;
using SendGrid;
using SendGrid.Helpers.Mail;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using static Snipster.Helpers.GeneralHelpers;
using System.Text.RegularExpressions;

namespace Snipster.Services
{
    public interface IEmailService
    {
        public Task SendEmailNotification(EmailSendingClass emailDetails);
    }
    public class EmailService : IEmailService
    {
        private readonly ISendGridClient _sendGridClient;
        [Inject] IConfiguration _configuration { get; set; }

        public EmailService(ISendGridClient sendGridClient, IConfiguration configuration)
        {
            _sendGridClient = sendGridClient;
            _configuration = configuration;
        }

        public async Task SendEmailNotification(EmailSendingClass emailDetails)
        {
            var fromEmail = Environment.GetEnvironmentVariable("SENDGRID_FROM_EMAIL") ?? "noreply@snipster.co";
            var fromName = Environment.GetEnvironmentVariable("SENDGRID_FROM_NAME") ?? "Snipster team";

            var from = new EmailAddress(fromEmail, fromName);
            var subject = emailDetails.Subject;
            var to = new EmailAddress(emailDetails.To);

            var test = Environment.GetEnvironmentVariable("Environment");


            //var plainTextContent = $"Click the link to reset your password: {resetUrl}";


            var plainTextContent = string.IsNullOrWhiteSpace(emailDetails.PlainTextContent)
                ? Regex.Replace(emailDetails.htmlContent ?? "", "<.*?>", " ")
                : emailDetails.PlainTextContent;

            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, emailDetails.htmlContent);
            var response = await _sendGridClient.SendEmailAsync(msg);
            var res = await response.Body.ReadAsStringAsync();
        }

    }
}
