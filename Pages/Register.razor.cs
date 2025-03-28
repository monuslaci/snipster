using Microsoft.AspNetCore.Components;
using Snipster.Services;
using System;
using System.Net.NetworkInformation;
using System.Reflection;
using Snipster.Components;
using static Snipster.Data.DBContext;
using Microsoft.JSInterop;
using System.Linq;
using Microsoft.AspNetCore.Components.Forms;
using MongoDB.Driver;
using static Snipster.Data.CommonClasses;
using Blazored.Toast.Services;
using static Snipster.Helpers.GeneralHelpers;
using System.Text.RegularExpressions;

namespace Snipster.Pages
{
    public partial class Register
    {
        [Inject] Blazored.Toast.Services.IToastService ToastService { get; set; }
        [Inject] EmailService EmailService { get; set; }
        private RegisterUserDTO newUser = new RegisterUserDTO();
        private string Password { get; set; }
        private string? Message { get; set; }

        private async Task HandleRegister()
        {
            var user = new Users
            {
                FirstName = newUser.FirstName,
                LastName = newUser.LastName,
                //UserName = newUser.UserName,
                Email = newUser.Email,
                RegistrationConfirmed = false
                
            };

            var result = await MongoDbService.RegisterUserAsync(user, newUser.Password);

            if (result)
            {
                Message = "Registration successful! Redirecting to login...";
                ToastService.ShowSuccess("Registration successful, please confirm your registration by clicking on the link in the email sent to your email address! Redirecting to login...");

                string token = await MongoDbService.GenerateResetTokenAsync(user.Email);
                await EmailService.SendEmailNotification(CreateRegisterEmailTemplate(user.Email, $"{user.FirstName} {user.LastName}", token));

                user.RegistrationConfirmed = true;
                await MongoDbService.UpdateUser(user);

                await Task.Delay(2000);
                Navigation.NavigateTo("/login");
            }
            else
            {
                //Message = string.Join(" ", result.Errors.Select(e => e.Description));
                Message = "Registration in not successful!";
                ToastService.ShowError("Registration in not successful, this email address is already registered");
            }
        }

        private EmailSendingClass CreateRegisterEmailTemplate(string email, string name, string token)
        {
            EmailSendingClass emailDetails = new EmailSendingClass();

            var url = "";
            if (Environment.GetEnvironmentVariable("Environment") == "Development")
                url = $"https://localhost:7225/validate-registration?token={token}";
            else if (Environment.GetEnvironmentVariable("Environment") == "Production")
                url = $"https://yourapp.com/pvalidate-registration?token={token}";

            RegistrationEmailTemplate = Regex.Replace(RegistrationEmailTemplate, "<url>", url);
            RegistrationEmailTemplate = Regex.Replace(RegistrationEmailTemplate, "<Name>", name);

            emailDetails.htmlContent = RegistrationEmailTemplate;
            emailDetails.To = email;
            emailDetails.Subject = "Confirm your registration on Snipster.com";

            return emailDetails;
        }

        public string RegistrationEmailTemplate = @"
                <!DOCTYPE html> <html> <head> <style> p { margin: 0;} OL { list-style-type: decimal; } OL OL  {list-style-type: upper-roman;} UL  {list-style-type: disc;} UL UL  {list-style-type: square;} .cal {font: 15px Calibri;} </style> </head><body>
                <body>
                <div><p>Dear <Name>, </p> <p> <o:p>&nbsp;</o:p></p>
                <p>To confirm your registration on Snipster.com, please click on this <a href='<resetUrl>'>link</a> </p> <p><o:p>&nbsp;</o:p></p>

                <p>If you didn’t request this, please ignore this email.</p> <p><o:p>&nbsp;</o:p></p>

                <p>Best regards,</p> 
                <p>Snipster Team</p><p><o:p>&nbsp;</o:p></p>
                </body>
                ";
    

    }




}

