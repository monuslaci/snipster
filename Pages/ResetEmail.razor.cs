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

namespace Snipster.Pages
{
    public partial class ResetEmail
    {
        [Inject] Blazored.Toast.Services.IToastService ToastService { get; set; }
        private ResetModel resetEmailModel = new ResetModel();
        private bool emailSent = false;

        private async Task HandleResetEmail()
        {
            // Fetch the user's email from the database (mocked here)
            var user = await MongoDbService.GetUser(resetEmailModel.Email);

            if (user != null)
            {
                string token = await MongoDbService.GenerateResetTokenAsync(user.Email);
                await EmailService.SendForgotEmailNotification(user.Email, token);
                

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

    }
}

