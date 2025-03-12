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

namespace Snipster.Pages
{
    public partial class Login
    {

        private LoginModel loginModel = new LoginModel();
        [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; }

        private async Task HandleLogin()
        {
            var result = await MongoDbService.ValidateUserAsync(loginModel.Email, loginModel.Password);

            if (result)
            {
                // Ensure we cast to CustomAuthenticationStateProvider
                if (AuthStateProvider is CustomAuthenticationStateProvider customAuthStateProvider)
                {
                    await customAuthStateProvider.MarkUserAsAuthenticated(loginModel.Email);
                }

                // Redirect to internal page after login
                await Task.Delay(2000);
                Navigation.NavigateTo("/");  // Redirect to the home page or other internal pages
            }
            else
            {
                // Handle login failure (e.g., show error message)
            }
        }

    }

    public class LoginModel
        {
            public string Email { get; set; }
            public string Password { get; set; }
        }


}

