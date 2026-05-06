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
using Snipster.Application.Accounts;

namespace Snipster.Pages
{
    public partial class ValidateRegistration
    {

        [Parameter] public string Token { get; set; } // Extract token from URL
        [Inject] Blazored.Toast.Services.IToastService ToastService { get; set; }
        [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; }
        [Inject] IAccountService AccountService { get; set; }
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

            isSuccess = await AccountService.ConfirmRegistrationAsync(Token);
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
            var result = await AccountService.ResendRegistrationConfirmationAsync(resetEmailModel.Email);

            if (result.Success)
            {
                ToastService.ShowSuccess(result.Message);
                await Task.Delay(2000);
                Navigation.NavigateTo("/login");
            }
            else
            {
                ToastService.ShowError(result.Message);
            }
        }
    }

}




