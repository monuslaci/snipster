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
using Snipster.Application.Accounts;

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
        [Inject] IAccountService AccountService { get; set; }
        [Inject] NavigationManager Navigation { get; set; }
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
            var loginResult = await AccountService.ValidateCredentialsAsync(loginModel.Email, loginModel.Password);
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
                var result = await AccountService.ResendRegistrationConfirmationAsync(loginModel.Email);

                if (result.Success)
                {
                    ToastService.ShowSuccess(result.Message);
                    showResendRegistrationConfirmation = false;
                    return;
                }

                ToastService.ShowError(result.Message);
            }
            finally
            {
                isResendingRegistrationConfirmation = false;
            }
        }
         }

}






