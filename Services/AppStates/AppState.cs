using Microsoft.AspNetCore.Components;
using static Snipster.Data.DBContext;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using static Snipster.Services.AppStates.AppState;
using System.Diagnostics.Metrics;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Components.Authorization;
using Snipster.Components;
using System.Linq;
using Microsoft.AspNetCore.Components.Forms;
using MongoDB.Driver;
using static Snipster.Data.CommonClasses;
using static Snipster.Helpers.GeneralHelpers;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace Snipster.Services.AppStates
{
    public class AppState : BaseAppState
    {
        public System.Security.Claims.ClaimsPrincipal principal;
        private bool IsInitialized = false;
        public string? userEmail { get; set; }
        public Users? user { get; set; }
        public ProtectedBrowserStorageResult<string> storedUser { get; set; }
        public ProtectedBrowserStorageResult<string> storedExpiration { get; set; }
        public List<Collection> collections = new List<Collection>();
        public List<Collection> sharedCollections = new List<Collection>();
        private AuthenticationStateProvider _authStateProvider { get; set; }
        private MongoDbService _mongoDbService { get; set; }
        private ProtectedSessionStorage _sessionStorage { get; set; }
        public AppState(AuthenticationStateProvider _authStateProvider, MongoDbService _mongoDbService, ProtectedSessionStorage _sessionStorage)
        {
            this._authStateProvider = _authStateProvider;
            this._mongoDbService = _mongoDbService;
            this._sessionStorage = _sessionStorage;

        }

        public async Task InitializedAsync(ComponentBase source, ClaimsPrincipal authenticationState)
        {
            if (!IsInitialized)
            {
                if (authenticationState == null)
                {
                    return;
                }


               // await LoadCurrentUser();

                IsInitialized = true;
                NotifyStateChanged(source, "");
            }
        }

        public async Task GetUserDetailsFromSessionStorage()
        {
            try
            {
                storedUser = await _sessionStorage.GetAsync<string>("userEmail");
                storedExpiration = await _sessionStorage.GetAsync<string>("sessionExpiration");
            }
            catch (Exception ex)
            {

                try
                {

                }
                catch { }

            }
        }

        public async Task LoadCurrentUser(string email)
        {
            try
            {
                //var authState = await _authStateProvider.GetAuthenticationStateAsync();
                //userEmail = authState.User.Identity?.Name;

                user = await _mongoDbService.GetUser(email);
                userEmail = user.Email;
            }
            catch (Exception ex)
            {

                try
                {

                }
                catch { }

            }
        }

        public async Task GetUserFfromSessionStorage()
        {
            try
            {
                var authState = await _authStateProvider.GetAuthenticationStateAsync();
                userEmail = authState.User.Identity?.Name;

                user = await _mongoDbService.GetUser(userEmail);
                //userEmail = user.Email;
            }
            catch (Exception ex)
            {

                try
                {

                }
                catch { }

            }
        }

        public async Task LoadCollections()
        {
            try
            {

                //get the user's collections
                collections = await _mongoDbService.GetCollectionsForUserAsync(userEmail);
            }
            catch (Exception ex)
            {

                try
                {

                }
                catch { }

            }
        }

        public async Task LoadSharedCollections()
        {
            try
            {
                //get the collections that are shared with them
                sharedCollections = await _mongoDbService.GetSharedCollectionsForUserAsync(userEmail);
            }
            catch (Exception ex)
            {

                try
                {

                }
                catch { }

            }
        }

    }

}
