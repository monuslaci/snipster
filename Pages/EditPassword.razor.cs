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

namespace Snipster.Pages
{
    public partial class EditPassword
    {
        [Inject] Blazored.Toast.Services.IToastService ToastService { get; set; }
        [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; }
        [Inject] NavigationManager Navigation { get; set; }
        [Inject] MongoDbService _mongoDbService { get; set; }
        private string? userEmail { get; set; }
        private Modal spinnerModal = new Modal();
        private ChangePwModel editPassword = new ChangePwModel();
        private Users user = new Users();

        protected override async Task OnInitializedAsync()
        {


        }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                spinnerModal.IsSpinner = true;
                spinnerModal.ShowModal();
                var authState = await AuthStateProvider.GetAuthenticationStateAsync();
                userEmail = authState.User.Identity?.Name;
                user = await _mongoDbService.GetUser(userEmail);

                spinnerModal.CloseModal();

            }

        }
        private async Task HandleSave()
        {
            if (editPassword.Password != editPassword.PasswordRepeat)
            {
                ToastService.ShowError("New password must match the repeated password.");
                Cancel();
            }


            spinnerModal.ShowModal();

            await _mongoDbService.UpdateUserWithPassword(user, editPassword.Password);

            spinnerModal.CloseModal();

            ToastService.ShowSuccess("Successfully changed the password.");

   
            //ToastService.ShowError("Changing the user details was unsuccessful.");
        }
        private void Cancel()
        {
            Navigation.NavigateTo("/");
        }

    }

}




