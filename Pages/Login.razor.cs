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
    public partial class Login
    {

        private LoginModel loginModel = new LoginModel();
        [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; }
        [Inject] Blazored.Toast.Services.IToastService ToastService { get; set; }
        private bool _isInitialized = false;

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

            if (loginResult.Result)
            {
                // Ensure we cast to CustomAuthenticationStateProvider
                if (AuthStateProvider is CustomAuthenticationStateProvider customAuthStateProvider)
                {
                    await customAuthStateProvider.MarkUserAsAuthenticated(loginModel.Email);
                }

                // Redirect to internal page after login
                await Task.Delay(2000);
                Navigation.NavigateTo("/");  // Redirect to the home page 
            }
            else
            {
                ToastService.ShowError($"{loginResult.Description}");
            }
        }

    }




}

