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

namespace Snipster.Pages
{
    public partial class ValidateRegistration
    {

        [Parameter] public string Token { get; set; } // Extract token from URL
        [Inject] Blazored.Toast.Services.IToastService ToastService { get; set; }
        private RegisterUserDTO newUser { get; set; }

        private bool isProcessing = false;
        private bool isSuccess = false;

        protected override async Task OnInitializedAsync()
        {
            // Extract token from URL
            Token = Navigation.ToAbsoluteUri(Navigation.Uri).Query.Split("token=")[1];


            bool success = await _mongoDbService.ValidateGeneratedRegisterTokenAsync(Token);

            if (success)
            {
                isSuccess = true;
                //ToastService.ShowSuccess($"Registration is confirmed");
                await Task.Delay(2000);
                Navigation.NavigateTo("/login"); // Redirect to login after reset
            }
            else
            {
                ToastService.ShowError($"Invalid token");
            }
            isProcessing = false;
        }

    }




}

