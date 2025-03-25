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
            var from = new EmailAddress("laszlo.monus@gmail.com", "Snipster team");
            var subject = emailDetails.Subject;
            var to = new EmailAddress(emailDetails.To);

            var test = Environment.GetEnvironmentVariable("Environment");


            //var plainTextContent = $"Click the link to reset your password: {resetUrl}";


            var msg = MailHelper.CreateSingleEmail(from, to, subject, "", emailDetails.htmlContent);
            var response = await _sendGridClient.SendEmailAsync(msg);
            var res = await response.Body.ReadAsStringAsync();
        }

    }
}
