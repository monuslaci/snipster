﻿using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Snipster.Components;
using Snipster.Helpers;
using Snipster.Services;
using Snipster.Services.AppStates;
using System.Collections.Generic;
using static Snipster.Data.DBContext;


namespace Snipster.Pages
{
    public partial class SnippetManagement
    {
        [Inject] MongoDbService MongoDbService { get; set; }
        [Inject] NavigationManager Navigation { get; set; }
        [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; }
        [Inject] private AppState _appState { get; set; }
        [Inject] private IGeneralHelpers _helper { get; set; }
        private bool IncludeSharedSnippets { get; set; } = false;
        private string searchQuery { get; set; }
        private List<Snippet> filteredSnippets = new List<Snippet>();
        private Dictionary<string, IEnumerable<string>> relatedCollections = new Dictionary<string, IEnumerable<string>>();
        private List<Collection> allCollections = new List<Collection>(); // List of all available collections for user
        private List<string> selectedCollectionIds = new List<string>(); // Stores selected collection IDs
        private string searchSnippetQuery { get; set; }
        private Modal spinnerModal { get; set; }
        private Snippet selectedSnippet { get; set; }
        private string userEmail { get; set; }
        private bool IsFavouriteSearch { get; set; }
        private int currentPage = 1;
        private int pageSize = 10;
        private IEnumerable<Snippet> PagedSnippets => filteredSnippets
                                                        .Skip((currentPage - 1) * pageSize)
                                                        .Take(pageSize);

        private int TotalPages => (int) Math.Ceiling((double) (filteredSnippets?.Count() ?? 0) / pageSize);

        protected override async Task OnInitializedAsync()
        {
            //loggedInUser = await SessionStorage.GetAsync<string>("userEmail");
            //var loggedInUserEmail = !string.IsNullOrEmpty(loggedInUser.Value) ? loggedInUser.Value.ToString() : "";

     
        }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                userEmail = _appState.userEmail;

                spinnerModal.IsSpinner = true;
                spinnerModal.ShowModal();
                await LoadSnippets();
                StateHasChanged();
                spinnerModal.CloseModal();
            }

        }
        private async Task LoadSnippets()
        {
            //load own colletions to appstate
            if (!_appState.collections.Any())
                await _appState.LoadCollections();

            //load collections shared with user to appstate
            if (!_appState.sharedCollections.Any())
                await _appState.LoadSharedCollections();

            //load own snippets
            //filteredSnippets = await MongoDbService.GetSnippetsByUserAsync(_appState.user, _appState.collections);
            foreach (var ls in _appState.loadedSnippets)
            {
                filteredSnippets.AddRange(ls.snippetList);

            }
            //foreach (var lss in _appState.loadedSharedSnippets)
            //{
            //    snippets.AddRange(lss.snippetList);
            //}

            if (IncludeSharedSnippets)
            {
                List<Snippet> filteredSharedSnippets = new List<Snippet>();
                //load shared snippets
                foreach (var ls in _appState.loadedSnippets)
                {
                    filteredSharedSnippets.AddRange(ls.snippetList);

                }
                //List<Snippet> filteredSharedSnippets = await MongoDbService.GetSharedSnippetsByUserAsync(_appState.user);
                filteredSnippets.AddRange(filteredSharedSnippets);
            }

            await LoadRelatedCollections();
        }

        private async Task OpenSnippet(string snippetId)
        {
            selectedSnippet = await MongoDbService.GetSnippetByIdAsync(snippetId);
            var collection = await MongoDbService.GetCollectionsBySnippetId(selectedSnippet.Id);
            var selectedCollectionId = collection != null && collection.Count()>0 ? collection.FirstOrDefault().Id : "";
            Navigation.NavigateTo($"/collections?selectedCollectionId={selectedCollectionId}&selectedSnippetId={selectedSnippet.Id}");
        }
        private async Task<IEnumerable<string>> GetRelatedCollections(string snippetId)
        {
            //var collections = await MongoDbService.GetCollectionsBySnippetId(snippetId);
            //return collections.Select(c => c.Title);

            return allCollections
              .Where(c => c.SnippetIds.Contains(snippetId))
              .Select(c => c.Title);
        }

        private async Task LoadRelatedCollections()
        {
            allCollections = _appState.collections.Concat(_appState.sharedCollections).ToList();

            // Load related collections for each snippet
            foreach (var snippet in filteredSnippets)
            {
                var collections = await GetRelatedCollections(snippet.Id);
                relatedCollections[snippet.Id] = collections;
            }
        }

        private async Task SearchFavouriteSnippets()
        {
            IsFavouriteSearch = !IsFavouriteSearch;
            //currentPage = 1;
            await OnSearchSnippet();
        }

        private async Task ToggleSharedSnippets()
        {
            IncludeSharedSnippets = !IncludeSharedSnippets;
            await OnSearchSnippet();
        }

        private async Task OnSearchSnippet()
        {
            spinnerModal.ShowModal();

            filteredSnippets.Clear();

            // Instead of clearing first, directly assign the new filtered list
            //search in the own collections
            //var results = await MongoDbService.SearchSnippetAsync(searchSnippetQuery, _appState.user, _appState.collections, IsFavouriteSearch);
            var results = _helper.SearchSnippetFromMemory(searchSnippetQuery, _appState.user, _appState.collections, IsFavouriteSearch);

            //search in the shared snippets
            if (IncludeSharedSnippets)
            {
                List<Snippet> sharedResults = _helper.SearchSharedSnippetFromMemory(searchSnippetQuery, _appState.user, IsFavouriteSearch);
                results.AddRange(sharedResults);
            }

            // Assign new results directly
            filteredSnippets.AddRange(results);

            if (Math.Ceiling((double) filteredSnippets.Count() / 10) < currentPage)
                currentPage = 1;

            StateHasChanged();
            spinnerModal.CloseModal();
        }
        private async Task CancelSearchSnippet()
        {
            //load own colletions
            if (!_appState.collections.Any())
                await _appState.LoadCollections();

            //load own snippets
            filteredSnippets = await MongoDbService.GetSnippetsByUserAsync(_appState.user, _appState.collections);

            searchSnippetQuery = "";
            IsFavouriteSearch = false;
            StateHasChanged();
        }

        private void GoToPage(int page)
        {
            if (page >= 1 && page <= TotalPages)
            {
                currentPage = page;
            }
        }

        private void NextPage() => GoToPage(currentPage + 1);
        private void PreviousPage() => GoToPage(currentPage - 1);
    }
}
