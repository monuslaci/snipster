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
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using static Snipster.Data.CommonClasses;
using Blazored.Toast.Services;
using static Snipster.Helpers.GeneralHelpers;
using System.Text.RegularExpressions;
using AspNetCore.Identity.MongoDbCore.Models;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Snipster.Services.AppStates;

namespace Snipster.Pages
{
    public partial class ResetEmail
    {
        [Inject] Blazored.Toast.Services.IToastService ToastService { get; set; }
        [Inject] MongoDbService MongoDbService { get; set; }
        [Inject] NavigationManager Navigation { get; set; }
        [Inject] EmailService EmailService { get; set; }
        [Inject] private AppState _appState { get; set; }
        private ResetModel resetEmailModel = new ResetModel();
        private bool emailSent { get; set; } = false;
        

        private async Task HandleResetEmail()
        {
            // Fetch the user's email from the database (mocked here)
            var user = _appState.user;

            if (user != null)
            {
                string token = await MongoDbService.GenerateResetTokenAsync(user.Email);


              
                await EmailService.SendEmailNotification(CreateResetEmailTemplate(user.Email, $"{user.FirstName} {user.LastName}", token));

                ToastService.ShowSuccess($"An email has been sent to your registered email address");
                await Task.Delay(2000);
                Navigation.NavigateTo("/login");

            }
            else
            {
                ToastService.ShowError($"User with this email address is not registered");
                emailSent = false;
            }
        }

        private EmailSendingClass CreateResetEmailTemplate(string email, string name, string token) 
        {
            EmailSendingClass emailDetails = new EmailSendingClass();

            var resetUrl = "";
            if (Environment.GetEnvironmentVariable("Environment") == "Development")
                resetUrl = $"https://localhost:7225/pw-reset?token={token}";
            else if (Environment.GetEnvironmentVariable("Environment") == "Production")
                resetUrl = $"https://snipster.co/pw-reset?token={token}";

            ResetEmailTemplate = Regex.Replace(ResetEmailTemplate, "<resetUrl>", resetUrl);
            ResetEmailTemplate = Regex.Replace(ResetEmailTemplate, "<Name>", name);

            emailDetails.htmlContent = ResetEmailTemplate;
            emailDetails.To = email;
            emailDetails.Subject = "Reset Your Password in Snipster.com"; 

            return emailDetails;
        }
        public string ResetEmailTemplate = @"
                <!DOCTYPE html> <html> <head> <style> p { margin: 0;} OL { list-style-type: decimal; } OL OL  {list-style-type: upper-roman;} UL  {list-style-type: disc;} UL UL  {list-style-type: square;} .cal {font: 15px Calibri;} </style> </head><body>
                <body>
                <div><p>Dear <Name>, </p> <p> <o:p>&nbsp;</o:p></p>
                <p>You received this email because you requested a password reset.</p> <p><o:p>&nbsp;</o:p></p>
                <p>Click <a href='<resetUrl>' here</a> to reset your password.</p> <p><o:p>&nbsp;</o:p></p>
                <p>If you didn’t request this, please ignore this email. This link will expire in 24 hours.</p> <p><o:p>&nbsp;</o:p></p>

                <p>Best regards,</p> 
                <p>Snipster Team</p><p><o:p>&nbsp;</o:p></p>
                </body>
                ";
        }




}

