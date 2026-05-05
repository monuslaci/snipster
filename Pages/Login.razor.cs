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
using static System.Runtime.InteropServices.JavaScript.JSType;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Snipster.Services.AppStates;

namespace Snipster.Pages
{
    public partial class Login
    {

        private LoginModel loginModel = new LoginModel();
        private const string RegistrationNotConfirmedMessage = "Please confirm your registration before logging in.";
        private bool showResendRegistrationConfirmation = false;
        private bool isResendingRegistrationConfirmation = false;
        [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; }
        [Inject] Blazored.Toast.Services.IToastService ToastService { get; set; }
        [Inject] MongoDbService MongoDbService { get; set; }
        [Inject] NavigationManager Navigation { get; set; }
        [Inject] EmailService EmailService { get; set; }
        [Inject] ProtectedSessionStorage SessionStorage { get; set; }
        [Inject] private AppState _appState { get; set; }

        private bool _isInitialized = false;

        protected override async Task OnInitializedAsync()
        { }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender && !_isInitialized)
            {
                var storedUser = await SessionStorage.GetAsync<string>("userEmail");
                var storedExpiration = await SessionStorage.GetAsync<string>("sessionExpiration");



                if (storedUser.Success && !string.IsNullOrEmpty(storedUser.Value) &&
                    storedExpiration.Success && DateTime.TryParse(storedExpiration.Value, out var expirationTime))
                {
                    if (DateTime.UtcNow < expirationTime) // Check if session is still valid
                    {
                        if (AuthStateProvider is CustomAuthenticationStateProvider customAuthStateProvider)
                        {
                            await customAuthStateProvider.MarkUserAsAuthenticated(storedUser.Value);

                            StateHasChanged();

                            await Task.Delay(2000);
                            Navigation.NavigateTo("/");  
                        }
                    }
                    else
                    {
                        // Session expired, clear storage
                        await SessionStorage.DeleteAsync("userEmail");
                        await SessionStorage.DeleteAsync("sessionExpiration");
                    }
                }
            }
        }


        private async Task HandleLogin()
        {
            var loginResult = await MongoDbService.ValidateUserAsync(loginModel.Email, loginModel.Password);
            showResendRegistrationConfirmation = false;

            if (loginResult.Result)
            {
                // Ensure we cast to CustomAuthenticationStateProvider
                if (AuthStateProvider is CustomAuthenticationStateProvider customAuthStateProvider)
                {
                    await customAuthStateProvider.MarkUserAsAuthenticated(loginModel.Email);
                }

                await _appState.LoadCurrentUser(loginModel.Email);
                var user = _appState.user;
                //var user = await MongoDbService.GetUser(loginModel.Email);
                //await EmailService.SendEmailNotification(CreateLoginEmailTemplate(user.Email, $"{user.FirstName} {user.LastName}"));

                // Redirect to internal page after login
                Navigation.NavigateTo("/");  // Redirect to the home page 
            }
            else
            {
                showResendRegistrationConfirmation = loginResult.Description == RegistrationNotConfirmedMessage;
                ToastService.ShowError($"{loginResult.Description}");
            }
        }

        private async Task HandleResendRegistrationConfirmation()
        {
            isResendingRegistrationConfirmation = true;

            try
            {
                var user = await MongoDbService.GetUser(loginModel.Email);

                if (user == null)
                {
                    ToastService.ShowError("Email address is not registered");
                    return;
                }

                if (user.RegistrationConfirmed)
                {
                    ToastService.ShowSuccess("Registration is already confirmed. You can log in now.");
                    showResendRegistrationConfirmation = false;
                    return;
                }

                if (string.IsNullOrWhiteSpace(user.Email))
                {
                    ToastService.ShowError("User email address is missing.");
                    return;
                }

                string token = await MongoDbService.GenerateRegisterTokenAsync(user.Email);
                await EmailService.SendEmailNotification(CreateRegisterEmailTemplate(user.Email, $"{user.FirstName} {user.LastName}", token));

                ToastService.ShowSuccess("Activation email has been sent again. If you do not see it, check your spam or junk folder.");
            }
            finally
            {
                isResendingRegistrationConfirmation = false;
            }
        }

        private EmailSendingClass CreateRegisterEmailTemplate(string email, string name, string token)
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

        private EmailSendingClass CreateLoginEmailTemplate(string email, string name)
        {
            EmailSendingClass emailDetails = new EmailSendingClass();

            var url = "";
            if (Environment.GetEnvironmentVariable("Environment") == "Development")
                url = $"https://localhost:7225";
            else if (Environment.GetEnvironmentVariable("Environment") == "Production")
                url = $"https://snipster.co";

            LoginEmailTemplate = Regex.Replace(LoginEmailTemplate, "<url>", url);
            LoginEmailTemplate = Regex.Replace(LoginEmailTemplate, "<Name>", name);

            emailDetails.htmlContent = LoginEmailTemplate;
            //emailDetails.To = email;
            emailDetails.To = "monuslaci@gmail.com";
            emailDetails.Subject = "Test email at login from Snipster.com";

            return emailDetails;
        }
        public string LoginEmailTemplate = @"
                <!DOCTYPE html> <html> <head> <style> p { margin: 0;} OL { list-style-type: decimal; } OL OL  {list-style-type: upper-roman;} UL  {list-style-type: disc;} UL UL  {list-style-type: square;} .cal {font: 15px Calibri;} </style> </head><body>
                <body>
                <div><p>Dear <Name>, </p> <p> <o:p>&nbsp;</o:p></p>
                <p>You received this email because you logged in from this URL: <url>.</p> <p><o:p>&nbsp;</o:p></p>

                <p>Best regards,</p> 
                <p>Snipster Team</p><p><o:p>&nbsp;</o:p></p>
                </body>
                ";
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






