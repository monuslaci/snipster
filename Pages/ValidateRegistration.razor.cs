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
        private bool isProcessing = false;
        private bool isSuccess = false;
        private string? userEmail { get; set; }
        private Modal spinnerModal { get; set; }
        private Users user { get; set; }

        protected override async Task OnInitializedAsync()
        {
            // Extract token from URL
            Token = Navigation.ToAbsoluteUri(Navigation.Uri).Query.Split("token=")[1];


        }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                spinnerModal.ShowModal();

                userEmail = _appState.userEmail;
                user = _appState.user;

                isProcessing = true;
                bool success = await _mongoDbService.ValidateGeneratedRegisterTokenAsync(Token);

                if (success)
                {
                    isSuccess = true;
           
                    user.RegistrationConfirmed = true;
                    await _mongoDbService.UpdateUser(user);
                    isProcessing = false;
                    //ToastService.ShowSuccess($"Registration is confirmed, you are now redirected to the login page..");

                    await Task.Delay(3000);
                    Navigation.NavigateTo("/login"); 
                }
                else
                {
                    ToastService.ShowError($"Invalid token");
                }
                

                spinnerModal.CloseModal();
            }

        }
        private async Task HandleResetEmail()
        {
            if (user != null)
            {
                string token = await _mongoDbService.GenerateResetTokenAsync(user.Email);

                await EmailService.SendEmailNotification(CreateResetEmailTemplate(user.Email, $"{user.FirstName} {user.LastName}", token));

                ToastService.ShowSuccess($"An email has been sent to your registered email address, please click on the link to confirm the registration");

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




