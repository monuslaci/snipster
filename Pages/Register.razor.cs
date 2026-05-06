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
using static Snipster.Data.CommonClasses;
using Blazored.Toast.Services;
using static Snipster.Helpers.GeneralHelpers;
using System.Text.RegularExpressions;
using Snipster.Application.Accounts;

namespace Snipster.Pages
{
    public partial class Register
    {
        [Inject] Blazored.Toast.Services.IToastService ToastService { get; set; }
        [Inject] IAccountService AccountService { get; set; }
        private RegisterUserDTO newUser = new RegisterUserDTO();
        private string Password { get; set; }
        private string? Message { get; set; }

        private async Task HandleRegister()
        {
            var result = await AccountService.RegisterAsync(newUser);

            if (result.Success)
            {
                Message = "Registration successful! Redirecting to login...";
                ToastService.ShowSuccess(result.Message);

                await Task.Delay(2000);
                Navigation.NavigateTo("/login");
            }
            else
            {
                //Message = string.Join(" ", result.Errors.Select(e => e.Description));
                Message = "Registration in not successful!";
                ToastService.ShowError(result.Message);
            }
        }
    

    }




}

