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
using AspNetCore.Identity.MongoDbCore.Models;
using static Snipster.Helpers.GeneralHelpers;
using System.Text.RegularExpressions;
using Amazon.Runtime.Internal.Endpoints.StandardLibrary;
using Snipster.Services.AppStates;
using Microsoft.AspNetCore.WebUtilities;

namespace Snipster.Pages
{
    public partial class ValidateRegistration
    {

        [Parameter] public string Token { get; set; } // Extract token from URL
        [Inject] Blazored.Toast.Services.IToastService ToastService { get; set; }
        [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; }
        [Inject] EmailService EmailService { get; set; }
        [Inject] private AppState _appState { get; set; }
        private ResetModel resetEmailModel = new ResetModel();
        private bool isProcessing = true;
        private bool isSuccess = false;
        private bool redirectStarted = false;

        protected override async Task OnInitializedAsync()
        {
            var query = QueryHelpers.ParseQuery(Navigation.ToAbsoluteUri(Navigation.Uri).Query);
            Token = query.TryGetValue("token", out var token) ? token.ToString() : "";

            if (string.IsNullOrWhiteSpace(Token))
            {
                isProcessing = false;
                ToastService.ShowError($"Invalid token");
                return;
            }

            isSuccess = await _mongoDbService.ValidateGeneratedRegisterTokenAsync(Token);
            isProcessing = false;

            if (!isSuccess)
            {
                ToastService.ShowError($"Invalid token");
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (isSuccess && !isProcessing && !redirectStarted)
            {
                redirectStarted = true;
                await Task.Delay(3000);
                Navigation.NavigateTo("/login");
            }
        }

        private async Task HandleResetEmail()
        {
            var user = await _mongoDbService.GetUser(resetEmailModel.Email);

            if (user != null)
            {
                string token = await _mongoDbService.GenerateRegisterTokenAsync(user.Email);

                await EmailService.SendEmailNotification(CreateResetEmailTemplate(user.Email, $"{user.FirstName} {user.LastName}", token));

                ToastService.ShowSuccess($"An email has been sent to your registered email address. If you do not see it, check your spam or junk folder.");

                await Task.Delay(2000);
                Navigation.NavigateTo("/login");

            }
            else
            {
                ToastService.ShowError($"User with this email address is not registered");
            }
        }

        private EmailSendingClass CreateResetEmailTemplate(string email, string name, string token)
        {
            EmailSendingClass emailDetails = new EmailSendingClass();

            var url = "";
            var encodedToken = Uri.EscapeDataString(token);
            if (Environment.GetEnvironmentVariable("Environment") == "Development")
                url = $"https://localhost:7225/validate-registration?token={encodedToken}";
            else if (Environment.GetEnvironmentVariable("Environment") == "Production")
                url = $"https://snipster.co/validate-registration?token={encodedToken}";

            var htmlContent = Regex.Replace(RegistrationEmailTemplate, "<url>", url);
            htmlContent = Regex.Replace(htmlContent, "<Name>", name);

            emailDetails.htmlContent = htmlContent;
            emailDetails.PlainTextContent = $"Dear {name}, confirm your registration on Snipster.com by opening this link: {url}. If you did not request this, please ignore this email.";
            emailDetails.To = email;
            emailDetails.Subject = "Confirm your registration on Snipster.com";

            return emailDetails;
        }

        public string RegistrationEmailTemplate = @"
                <!DOCTYPE html> <html> <head> <style> p { margin: 0;} OL { list-style-type: decimal; } OL OL  {list-style-type: upper-roman;} UL  {list-style-type: disc;} UL UL  {list-style-type: square;} .cal {font: 15px Calibri;} </style> </head><body>
                <body>
                <div><p>Dear <Name>, </p> <p> <o:p>&nbsp;</o:p></p>
                <p>To confirm your registration on Snipster.com, please click on this <a href='<url>'>link</a> </p> <p><o:p>&nbsp;</o:p></p>

                <p>If you cannot find this email later, please check your spam or junk folder.</p> <p><o:p>&nbsp;</o:p></p>

                <p>If you didn’t request this, please ignore this email.</p> <p><o:p>&nbsp;</o:p></p>

                <p>Best regards,</p> 
                <p>Snipster Team</p><p><o:p>&nbsp;</o:p></p>
                </body>
                ";
    }

}




