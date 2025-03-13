﻿using Microsoft.AspNetCore.Components;
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

namespace Snipster.Pages
{
    public partial class Register
    {

        private RegisterUserDTO newUser = new();
        private string Password { get; set; }
        private string Message { get; set; }

        private async Task HandleRegister()
        {
            var user = new Users
            {
                FirstName = newUser.FirstName,
                LastName = newUser.LastName,
                //UserName = newUser.UserName,
                Email = newUser.Email,
                
            };

            var result = await MongoDbService.RegisterUserAsync(user, newUser.Password);

            if (result)
            {
                Message = "Registration successful! Redirecting to login...";
                await Task.Delay(2000);
                Navigation.NavigateTo("/login");
            }
            else
            {
                //Message = string.Join(" ", result.Errors.Select(e => e.Description));
                Message = "Registration in not successful!";
            }
        }

    }




}

