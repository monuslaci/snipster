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
using Snipster.Application.Accounts;

namespace Snipster.Pages
{
    public partial class ResetEmail
    {
        [Inject] Blazored.Toast.Services.IToastService ToastService { get; set; }
        [Inject] NavigationManager Navigation { get; set; }
        [Inject] IAccountService AccountService { get; set; }
        [Inject] private AppState _appState { get; set; }
        private ResetModel resetEmailModel = new ResetModel();
        private bool emailSent { get; set; } = false;
        

        private async Task HandleResetEmail()
        {
            var result = await AccountService.SendPasswordResetAsync(resetEmailModel.Email);

            if (result.Success)
            {
                ToastService.ShowSuccess(result.Message);
                await Task.Delay(2000);
                Navigation.NavigateTo("/login");

            }
            else
            {
                ToastService.ShowError(result.Message);
                emailSent = false;
            }
        }
        }




}

