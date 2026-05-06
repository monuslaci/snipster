using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.JSInterop;
using Snipster.Components;
using Snipster.Helpers;
using Snipster.Application.Workspace;
using Snipster.Services;
using Snipster.Services.AppStates;
using System.Collections.Generic;
using static Snipster.Data.DBContext;


namespace Snipster.Pages
{
    public partial class SnippetManagement
    {
        [Inject] IWorkspaceService WorkspaceService { get; set; }
        [Inject] NavigationManager Navigation { get; set; }
        [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; }
        [Inject] private AppState _appState { get; set; }
        [Inject] private IGeneralHelpers _helper { get; set; }
        [Inject] private IJSRuntime JSRuntime { get; set; }
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
        private bool hasSearched { get; set; } = false;
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

                // Only load collection metadata (not snippets) if not already loaded
                // This is fast - just fetching collection names/IDs, no spinner needed
                if (!_appState.collections.Any())
                    _appState.collections = await WorkspaceService.GetUserCollectionsAsync(_appState.userEmail);
                if (!_appState.sharedCollections.Any())
                    _appState.sharedCollections = await WorkspaceService.GetSharedCollectionsAsync(_appState.userEmail);
                
                StateHasChanged();
            }

        }
        private async Task LoadSnippets()
        {
            // Load collection metadata only (not snippets) if not already loaded
            if (!_appState.collections.Any())
                _appState.collections = await WorkspaceService.GetUserCollectionsAsync(_appState.userEmail);

            // Load shared collection metadata only if not already loaded
            if (!_appState.sharedCollections.Any())
                _appState.sharedCollections = await WorkspaceService.GetSharedCollectionsAsync(_appState.userEmail);

            // Check if all snippets are already loaded
            bool allSnippetsLoaded = _appState.collections.All(c => 
                _appState.loadedSnippets.Any(ls => ls.collectionId == c.Id));

            if (!allSnippetsLoaded)
            {
                // Load ALL user snippets in a SINGLE database call
                var allSnippets = await WorkspaceService.GetUserSnippetsAsync(_appState.user, _appState.collections);

                // Distribute to memory cache by collection
                foreach (var collection in _appState.collections)
                {
                    if (!_appState.loadedSnippets.Any(ls => ls.collectionId == collection.Id))
                    {
                        var collectionSnippets = allSnippets
                            .Where(s => collection.SnippetIds.Contains(s.Id))
                            .ToList();

                        _appState.loadedSnippets.Add(new MemorySnippetList
                        {
                            collectionId = collection.Id,
                            snippetList = collectionSnippets
                        });
                    }
                }
            }

            // Populate filteredSnippets from memory
            foreach (var ls in _appState.loadedSnippets)
            {
                filteredSnippets.AddRange(ls.snippetList);
            }

            if (IncludeSharedSnippets)
            {
                // Load shared snippets in a single call
                bool allSharedLoaded = _appState.sharedCollections.All(c =>
                    _appState.loadedSharedSnippets.Any(ls => ls.collectionId == c.Id));

                if (!allSharedLoaded)
                {
                    var sharedSnippets = await WorkspaceService.GetSharedSnippetsAsync(_appState.user);

                    foreach (var collection in _appState.sharedCollections)
                    {
                        if (!_appState.loadedSharedSnippets.Any(ls => ls.collectionId == collection.Id))
                        {
                            var collectionSnippets = sharedSnippets
                                .Where(s => collection.SnippetIds.Contains(s.Id))
                                .ToList();

                            _appState.loadedSharedSnippets.Add(new MemorySnippetList
                            {
                                collectionId = collection.Id,
                                snippetList = collectionSnippets
                            });
                        }
                    }
                }

                foreach (var ls in _appState.loadedSharedSnippets)
                {
                    filteredSnippets.AddRange(ls.snippetList);
                }
            }

            LoadRelatedCollections();
        }

        private async Task OpenSnippet(string snippetId)
        {
            selectedSnippet = await WorkspaceService.GetSnippetAsync(snippetId);
            var collection = await WorkspaceService.GetCollectionsBySnippetIdAsync(selectedSnippet.Id);
            var selectedCollectionId = collection != null && collection.Count()>0 ? collection.FirstOrDefault().Id : "";
            Navigation.NavigateTo($"/collections?selectedCollectionId={selectedCollectionId}&selectedSnippetId={selectedSnippet.Id}");
        }
        private IEnumerable<string> GetRelatedCollections(string snippetId)
        {
            return allCollections
              .Where(c => c.SnippetIds.Contains(snippetId))
              .Select(c => c.Title);
        }

        private void LoadRelatedCollections()
        {
            allCollections = _appState.collections.Concat(_appState.sharedCollections).ToList();

            // Build lookup once for all snippets (much faster than per-snippet queries)
            relatedCollections.Clear();
            foreach (var snippet in filteredSnippets)
            {
                relatedCollections[snippet.Id] = GetRelatedCollections(snippet.Id);
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

        private async Task HandleSearchKeyUp(KeyboardEventArgs e)
        {
            if (e.Key == "Enter")
            {
                await OnSearchSnippet();
            }
        }

        private async Task OnSearchSnippet()
        {
            spinnerModal.IsSpinner = true;
            spinnerModal.ShowModal();

            // Load snippets if not already loaded
            await LoadSnippets();
            hasSearched = true;

            filteredSnippets.Clear();

            // Instead of clearing first, directly assign the new filtered list
            //search in the own collections
            var results = _helper.SearchSnippetFromMemory(searchSnippetQuery, _appState.user, _appState.collections, IsFavouriteSearch);

            //search in the shared snippets
            if (IncludeSharedSnippets)
            {
                List<Snippet> sharedResults = _helper.SearchSharedSnippetFromMemory(searchSnippetQuery, _appState.user, IsFavouriteSearch);
                results.AddRange(sharedResults);
            }

            // Assign new results directly
            filteredSnippets.AddRange(results);

            if (Math.Ceiling((double) filteredSnippets.Count() / pageSize) < currentPage)
                currentPage = 1;

            StateHasChanged();
            spinnerModal.CloseModal();
        }
        private void CancelSearchSnippet()
        {
            filteredSnippets.Clear();
            searchSnippetQuery = "";
            IsFavouriteSearch = false;
            hasSearched = false;
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
        private void FirstPage() => GoToPage(1);
        private void LastPage() => GoToPage(TotalPages);

        private async Task ScrollToTop()
        {
            await JSRuntime.InvokeVoidAsync("scrollToTop");
        }

        private async Task ScrollToBottom()
        {
            await JSRuntime.InvokeVoidAsync("scrollToBottom");
        }
    }
}
