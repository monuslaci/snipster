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
using Microsoft.AspNetCore.WebUtilities;
using Snipster.Application.Accounts;

namespace Snipster.Pages
{
    public partial class ResetPassword
    {

        private NewPwModel loginModel = new NewPwModel();
        [Parameter] public string Token { get; set; } // Extract token from URL
        [Inject] Blazored.Toast.Services.IToastService ToastService { get; set; }
        [Inject] IAccountService AccountService { get; set; }

        private bool isProcessing = false;
        private bool isSuccess = false;

        protected override void OnInitialized()
        {
            var query = QueryHelpers.ParseQuery(Navigation.ToAbsoluteUri(Navigation.Uri).Query);
            Token = query.TryGetValue("token", out var token) ? token.ToString() : "";
        }

        private async Task HandlePasswordReset()
        {
            isProcessing = true;

            bool success = await AccountService.ResetPasswordAsync(Token, loginModel.Password);

            if (success)
            {
                isSuccess = true;
                ToastService.ShowSuccess($"Password successfully changed.  Redirecting to login...");
                await Task.Delay(2000);
                Navigation.NavigateTo("/login"); // Redirect to login after reset
            }
            else
            {
                ToastService.ShowError($"Invalid or expired token");
            }
            isProcessing = false;
        }

    }




}

