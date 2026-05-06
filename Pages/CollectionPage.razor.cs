using Microsoft.AspNetCore.Components;
using Snipster.Services;
using Snipster.Components;
using static Snipster.Data.DBContext;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using static Snipster.Data.CommonClasses;
using Snipster.Services.AppStates;
using Snipster.Helpers;
using Snipster.Application.Workspace;

namespace Snipster.Pages
{
    public partial class CollectionPage
    {
        #region Dependencies

        [Inject] NavigationManager Navigation { get; set; }
        [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; }
        [Inject] IWorkspaceService WorkspaceService { get; set; }
        [Inject] private AppState _appState { get; set; }
        [Inject] private IGeneralHelpers _helper { get; set; }

        #endregion

        #region State

        private List<Collection> collections = new List<Collection>();
        private List<Collection> sharedCollections = new List<Collection>();
        private Collection newCollection = new Collection();
        private Collection editCollection = new Collection();
        private Snippet newSnippet = new Snippet();
        private List<Snippet> snippets = new List<Snippet>();
        private Snippet selectedSnippet { get; set; }
        private List<string> selectedSnippetOriginalSharedWith { get; set; }
        private Users user { get; set; }
        private bool isAddingSnippet { get; set; } = false; 
        private string? selectedCollectionId { get; set; }
        private string? selectedSnippetId { get; set; }
        private string? selectedCollectionIdCreate { get; set; }
        private string selectedCollectionIdEdit { get; set; }
        private Modal? createCollectionModal { get; set; }
        private Modal? editCollectionModal { get; set; }
        private Modal spinnerModal { get; set; }
        private bool adjustHeightNeeded { get; set; }
        private string HashtagsInput { get; set; }
        private List<string>? ValidHashtags = new List<string>();
        private string? userEmail { get; set; }
        private string? contentErrorMessage { get; set; }
        private string? searchCollectionQuery { get; set; }
        private string? searchSnippetQuery { get; set; }
        private bool isFormValid { get; set; } = true;
        private bool isLeftPanelOpen { get; set; } = true;
        private bool isMiddlePanelOpen { get; set; } = true;
        private string rightPanelWidth { get; set; } = "50%";
        private bool IsFavouriteSearch { get; set; }
        private bool selectedCollectionIsOwn { get; set; }
        private string isDisabled { get; set; } = "";
        private Dictionary<string, string> collectionOwnerNames = new(StringComparer.OrdinalIgnoreCase);
        private bool sharedCollectionsLoaded { get; set; }

        // Pagination for collections
        private int collectionsCurrentPage = 1;
        private int collectionsPageSize = 10;
        private string activeCollectionTab = "own";
        private IEnumerable<Collection> PagedCollections => collections
            .Skip((collectionsCurrentPage - 1) * collectionsPageSize)
            .Take(collectionsPageSize);
        private int CollectionsTotalPages => Math.Max(1, (int)Math.Ceiling((double)(collections?.Count ?? 0) / collectionsPageSize));

        // Pagination for shared collections
        private int sharedCollectionsCurrentPage = 1;
        private IEnumerable<Collection> PagedSharedCollections => sharedCollections
            .Skip((sharedCollectionsCurrentPage - 1) * collectionsPageSize)
            .Take(collectionsPageSize);
        private int SharedCollectionsTotalPages => Math.Max(1, (int)Math.Ceiling((double)(sharedCollections?.Count ?? 0) / collectionsPageSize));

        // Pagination for snippets
        private int snippetsCurrentPage = 1;
        private int snippetsPageSize = 10;
        private IEnumerable<Snippet> PagedSnippets => snippets
            .Skip((snippetsCurrentPage - 1) * snippetsPageSize)
            .Take(snippetsPageSize);
        private int SnippetsTotalPages => Math.Max(1, (int)Math.Ceiling((double)(snippets?.Count ?? 0) / snippetsPageSize));

        #endregion

        #region Lifecycle

        protected override async Task OnInitializedAsync()
        {

       
            var uri = Navigation.ToAbsoluteUri(Navigation.Uri);
            var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);

            if (query != null && query.TryGetValue("selectedCollectionId", out var id))
            {
                selectedCollectionId = id;
            }

            if (query != null && query.TryGetValue("selectedSnippetId", out var sid))
            {
                selectedSnippetId = sid;
            }

        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                spinnerModal.IsSpinner = true;
                spinnerModal.ShowModal();

                userEmail = _appState.userEmail;
                user = _appState.user;


                if (!_appState.collections.Any())
                    _appState.collections = await WorkspaceService.GetUserCollectionsAsync(_appState.userEmail);

                //get the user's collections
                collections = _appState.collections;

                if (_appState.sharedCollections.Any())
                {
                    sharedCollections = _appState.sharedCollections;
                    sharedCollectionsLoaded = true;
                }

                if (!string.IsNullOrEmpty(selectedCollectionId) && !collections.Any(c => c.Id == selectedCollectionId))
                {
                    await EnsureSharedCollectionsLoadedAsync();
                }

                await LoadCollectionOwnerNames();

                // Only load snippets if a collection is pre-selected (from URL)
                if (!string.IsNullOrEmpty(selectedCollectionId))
                    await LoadSnippets(selectedCollectionId);

                if (collections != null && !string.IsNullOrEmpty(selectedSnippetId))
                {
                    await LoadSnippetDetails(selectedSnippetId);
                }

                // Remove query parameter from the URL using JavaScript
                await JSRuntime.InvokeVoidAsync("updateUrlWithoutQueryParam", "/collections");
                spinnerModal.CloseModal();
                StateHasChanged();
                return; // Let the next render cycle handle editor initialization
            }
            
            if (adjustHeightNeeded)
            {
                adjustHeightNeeded = false; 
                // Re-initialize Monaco editor if needed
                if (selectedSnippet != null && !isAddingSnippet)
                {
                    await InitializeEditEditor();
                }
            }
        }

        #endregion

        #region Collection Actions
        private void ShowCreateCollectionModal()
        {
            newCollection = new Collection();
            newCollection.CreatedBy = userEmail;
            ValidHashtags.Clear();
            createCollectionModal.ShowModal();
        }

        private async Task HandleCreateCollection()
        {
            spinnerModal.ShowModal();


            await WorkspaceService.CreateCollectionAsync(newCollection, user, userEmail);

            _appState.collections.Add(newCollection);

            _appState.collections = _appState.collections
                .OrderBy(c => c.LastModifiedDate)
                .ToList();

            ////get the user's collections
            //await _appState.LoadCollections();

            collections = _appState.collections;

            //get the collections that are shared with them
            sharedCollections = _appState.sharedCollections;

            await LoadCollectionOwnerNames();

            StateHasChanged();

            createCollectionModal.CloseModal();
            spinnerModal.CloseModal();
        }
        private async Task HandleEditCollection()
        {
            ValidHashtags.Clear();
            spinnerModal.ShowModal();
            await WorkspaceService.UpdateCollectionAsync(editCollection, userEmail);
            editCollectionModal.CloseModal();

            //update the collection in the appstate collection list
            await _helper.UpdateCollectionInMemory(editCollection);

            //get the user's collections
            collections = _appState.collections;
            //get the collections that are shared with them
            sharedCollections = _appState.sharedCollections;

            await LoadCollectionOwnerNames();

            spinnerModal.CloseModal();
        }

        private async Task EditCollection(Collection collection)
        {
            editCollectionModal.ShowModal();
            editCollection = collection;
        }   

        private async Task DeleteCollection(string collectionId)
        {
            spinnerModal.ShowModal();
            await WorkspaceService.DeleteCollectionAsync(collectionId);

            await _helper.DeleteCollectionFromMemory(collectionId);

            //get the user's collections
            collections = _appState.collections;

            spinnerModal.CloseModal();
        }

        #endregion

        #region Snippet Loading

        private async Task LoadSnippets(string collectionId, string? snippetIdToSelect = null)
        {        
            spinnerModal.ShowModal();
            selectedCollectionId = collectionId;
            
            // Reset to first page when selecting a new collection (only if no specific snippet requested)
            if (snippetIdToSelect == null)
                snippetsCurrentPage = 1;

            if (sharedCollections != null && sharedCollections.Where(c => c.Id == selectedCollectionId).Count() == 0)
            {
                selectedCollectionIsOwn = true;
                isDisabled = "disabled";
            }
            else
            {
                selectedCollectionIsOwn = false;
                isDisabled = "";
            }
           

            // Clear the current snippets list and populate it with the new ones
            snippets.Clear();

            // Check if snippets are already loaded in memory
            bool foundInMemory = false;

            var cachedSnippetLists = selectedCollectionIsOwn
                ? _appState.loadedSnippets
                : _appState.loadedSharedSnippets;

            foreach (var ls in cachedSnippetLists) 
            {
                if (ls.collectionId == selectedCollectionId)
                {
                    snippets.AddRange(ls.snippetList);
                    foundInMemory = true;
                }
            }

            // If not in memory, fetch from database (lazy loading)
            if (!foundInMemory)
            {
                var dbSnippets = await WorkspaceService.GetCollectionSnippetsAsync(selectedCollectionId, selectedCollectionIsOwn, user);

                snippets.AddRange(dbSnippets);
                
                // Cache for future use
                cachedSnippetLists.Add(new MemorySnippetList
                {
                    collectionId = selectedCollectionId,
                    snippetList = dbSnippets
                });
            }

            selectedCollectionIdCreate = selectedCollectionId;
            
            // If a specific snippet was requested, use it; otherwise use first snippet
            string snippetToLoad = snippetIdToSelect ?? (snippets != null && snippets.Count > 0 ? snippets.FirstOrDefault()?.Id : "");
            await LoadSnippetDetails(snippetToLoad ?? "");


            StateHasChanged();
            spinnerModal.CloseModal();
        }
  
        private async Task LoadSnippetDetails(string snippetId)
        {
            // Clear any previous error message
            contentErrorMessage = null;
            
            // If no snippet ID provided, just clear the selection
            if (string.IsNullOrEmpty(snippetId))
            {
                selectedSnippet = null;
                isAddingSnippet = false;
                StateHasChanged();
                return;
            }

            spinnerModal.ShowModal();
            isAddingSnippet = false;
            
            // First try to find in local list, then fetch from DB if not found
            selectedSnippet = snippets?.Where(s => s.Id == snippetId).FirstOrDefault();
            
            if (selectedSnippet == null)
            {
                // Fetch from database if not in local list
                selectedSnippet = await WorkspaceService.GetSnippetAsync(snippetId);
            }

            if (selectedSnippet != null) 
                selectedSnippetOriginalSharedWith = selectedSnippet.SharedWith.ToList();

            spinnerModal.CloseModal();
            
            // Flag to initialize editor on next render cycle (when DOM exists)
            adjustHeightNeeded = true;
            StateHasChanged();
        }
        private async Task ShowSnippetFields()
        {
            // Reset the form model and show the new snippet fields
            newSnippet = new Snippet();
            selectedSnippet = null;
            isAddingSnippet = true;
            contentErrorMessage = null;
            StateHasChanged();
            
            // Initialize Monaco editor for creating
            await InitializeCreateEditor();
        }

        #endregion

        #region Snippet Actions

        private async Task EditSnippetFavourite(Snippet snippet)
        {
            snippet.IsFavourite = !snippet.IsFavourite;
            await WorkspaceService.SaveSnippetFavouriteAsync(snippet);
        }
        private async Task SearchFavouriteSnippets()
        {
            IsFavouriteSearch = !IsFavouriteSearch;
            await OnSearchSnippet();
        }
        
        private async Task HandleValidSubmitNew() //new snippet
        {
            // Get content from Monaco editor first
            newSnippet.Content = await GetCreateEditorContent();

            // Validate content manually since Monaco editor is separate from form
            if (string.IsNullOrWhiteSpace(newSnippet.Content))
            {
                contentErrorMessage = "Code content is required.";
                StateHasChanged();
                return;
            }
            contentErrorMessage = null;

            spinnerModal.ShowModal();

            if (selectedCollectionIdCreate == null)
                selectedCollectionIdCreate = selectedCollectionId;
            var snippetId = await WorkspaceService.CreateSnippetAsync(newSnippet, selectedCollectionIdCreate, user, collections);

            isAddingSnippet = false;

            await _helper.AddNewSnippetInMemory(selectedCollectionIdCreate, newSnippet, snippetId);

            await LoadSnippets(selectedCollectionId);

            selectedSnippet = newSnippet;
            adjustHeightNeeded = true;
            spinnerModal.CloseModal();
        }
        private async Task HandleValidSubmitEdit()
        {
            // Get content from Monaco editor first
            selectedSnippet.Content = await GetEditEditorContent();
            
            // Validate content manually since Monaco editor is separate from form
            if (string.IsNullOrWhiteSpace(selectedSnippet.Content))
            {
                contentErrorMessage = "Code content is required.";
                StateHasChanged();
                return;
            }
            contentErrorMessage = null;

            spinnerModal.ShowModal();
            
            await WorkspaceService.UpdateSnippetAsync(selectedSnippet, selectedCollectionId, collections, selectedSnippetOriginalSharedWith);

            isAddingSnippet = false;

            await _helper.UpdateSnippetInMemory(selectedCollectionId, selectedSnippet);
            
            // Reload snippets but stay on the same snippet
            await LoadSnippets(selectedCollectionId, selectedSnippet.Id);
            spinnerModal.CloseModal();
        }

        private async Task DeleteSnippet(string id)
        {
            spinnerModal.ShowModal();


            await WorkspaceService.DeleteSnippetAsync(id);

            await _helper.DeleteSnippetFromMemory(id);

            //reload the collection without the snippet
            await LoadSnippets(selectedCollectionId);

            var memorySnippetList = _appState.loadedSnippets.FirstOrDefault(x => x.collectionId == selectedCollectionId);
            var snippets = memorySnippetList?.snippetList ?? new List<Snippet>();
            var snippetIds = snippets.Select(s => s.Id).ToList();
            var snippetId = snippetIds.FirstOrDefault();

            await LoadSnippetDetails(snippetId);
            spinnerModal.CloseModal();
        }

        #endregion

        #region Search

        private async Task OnSearchCollection()
        {
            if (!string.IsNullOrEmpty(searchCollectionQuery))
            {
                // Reset to first page on search
                collectionsCurrentPage = 1;
                sharedCollectionsCurrentPage = 1;

                if (activeCollectionTab == "shared")
                {
                    await EnsureSharedCollectionsLoadedAsync();
                    sharedCollections = await _helper.SearchCollectionAsync(searchCollectionQuery, userEmail, _appState.sharedCollections);
                }
                else
                {
                    collections = await _helper.SearchCollectionAsync(searchCollectionQuery, userEmail, _appState.collections);
                }

                await LoadCollectionOwnerNames();

                if (activeCollectionTab == "own" && collections.Any())
                {
                    selectedCollectionId = collections.FirstOrDefault().Id;
                    await LoadSnippets(selectedCollectionId);
                }
                else if (activeCollectionTab == "shared" && sharedCollections.Any())
                {
                    selectedCollectionId = sharedCollections.FirstOrDefault().Id;
                    await LoadSnippets(selectedCollectionId);
                }

                StateHasChanged();
            }

        }

        private async Task OnSearchSnippet()
        {
            if (!string.IsNullOrEmpty(searchSnippetQuery))
            {
                spinnerModal.ShowModal();
                
                // Reset to first page on search
                snippetsCurrentPage = 1;
                
                //get the list of filtered snippets
                List<Snippet> filteredSnippetlist = await WorkspaceService.SearchCollectionSnippetsAsync(searchSnippetQuery, selectedCollectionId, IsFavouriteSearch, selectedCollectionIsOwn, user);

                // Clear the current snippets list and populate it with the new ones
                snippets.Clear();


                //betoltom az osszes szurt snppetet kozepre
                if (filteredSnippetlist != null && filteredSnippetlist.Count > 0)
                {
                    snippets.AddRange(filteredSnippetlist);

                    //kivalsztom az elso snippetet a listarol
                    string firstSnippetId = snippets != null ? snippets.FirstOrDefault().Id : "";


                    //betoltom annak a collectionjet bal oldalra
                    await LoadSnippetDetails(firstSnippetId);
                }



                StateHasChanged();
                spinnerModal.CloseModal();
            }        
        }

        private async Task CancelSearchCollection()
        {
            spinnerModal.ShowModal();

            // Reset pagination
            collectionsCurrentPage = 1;
            sharedCollectionsCurrentPage = 1;

            if (activeCollectionTab == "shared")
            {
                await EnsureSharedCollectionsLoadedAsync();
                sharedCollections = _appState.sharedCollections;
            }
            else
            {
                collections = _appState.collections;
            }

            await LoadCollectionOwnerNames();

            if (activeCollectionTab == "own" && collections.Any())
            {
                selectedCollectionId = collections.FirstOrDefault().Id;
                await LoadSnippets(selectedCollectionId);
            }
            else if (activeCollectionTab == "shared" && sharedCollections.Any())
            {
                selectedCollectionId = sharedCollections.FirstOrDefault().Id;
                await LoadSnippets(selectedCollectionId);
            }

            StateHasChanged();
            searchCollectionQuery = "";
            spinnerModal.CloseModal();
        }

        private async Task CancelSearchSnippet()
        {
           await LoadSnippets(selectedCollectionId);

            IsFavouriteSearch = false;
            searchSnippetQuery = "";
            StateHasChanged();
        }

        #endregion

        #region Monaco Editor

        // Monaco Editor methods
        private async Task InitializeCreateEditor()
        {
            await Task.Delay(100); // Wait for DOM to be ready
            await JSRuntime.InvokeVoidAsync("monacoEditor.createEditor", 
                "createEditorContainer", 
                newSnippet?.Language ?? "plaintext", 
                newSnippet?.Content ?? "", 
                false);
        }

        private async Task InitializeEditEditor()
        {
            if (selectedSnippet == null) return;
            
            // Wait for DOM to be ready after StateHasChanged
            await Task.Delay(200);
            await JSRuntime.InvokeVoidAsync("monacoEditor.createEditor", 
                "editEditorContainer", 
                selectedSnippet?.Language ?? "plaintext", 
                selectedSnippet?.Content ?? "", 
                !selectedCollectionIsOwn);
        }

        private async Task<string> GetCreateEditorContent()
        {
            return await JSRuntime.InvokeAsync<string>("monacoEditor.getValue", "createEditorContainer");
        }

        private async Task<string> GetEditEditorContent()
        {
            return await JSRuntime.InvokeAsync<string>("monacoEditor.getValue", "editEditorContainer");
        }

        // Language change handlers for Monaco editor
        private async Task OnNewLanguageChanged()
        {
            await JSRuntime.InvokeVoidAsync("monacoEditor.setLanguage", "createEditorContainer", newSnippet?.Language ?? "plaintext");
        }

        private async Task OnEditLanguageChanged()
        {
            await JSRuntime.InvokeVoidAsync("monacoEditor.setLanguage", "editEditorContainer", selectedSnippet?.Language ?? "plaintext");
        }

        #endregion

        #region Layout Toggles

        private void ToggleLeftPanel()
        {
            isLeftPanelOpen = !isLeftPanelOpen;
            UpdateRightPanelWidth();
        }

        private void ToggleMiddlePanel()
        {
            isMiddlePanelOpen = !isMiddlePanelOpen;
            UpdateRightPanelWidth();
        }

        private void UpdateRightPanelWidth()
        {
            int totalWidth = 100;
            int leftWidth = isLeftPanelOpen ? 20 : 5;
            int middleWidth = isMiddlePanelOpen ? 30 : 5;
            rightPanelWidth = $"{totalWidth - (leftWidth + middleWidth)}%";
        }

        #endregion

        #region Pagination And Tabs

        // Pagination methods for collections
        private void CollectionsNextPage()
        {
            if (collectionsCurrentPage < CollectionsTotalPages)
                collectionsCurrentPage++;
        }

        private void CollectionsPreviousPage()
        {
            if (collectionsCurrentPage > 1)
                collectionsCurrentPage--;
        }

        private void CollectionsFirstPage() => collectionsCurrentPage = 1;
        private void CollectionsLastPage() => collectionsCurrentPage = CollectionsTotalPages;

        private async Task SwitchCollectionTab(string tab)
        {
            activeCollectionTab = tab;
            if (tab == "own")
            {
                collectionsCurrentPage = 1;
            }
            else
            {
                sharedCollectionsCurrentPage = 1;
                await EnsureSharedCollectionsLoadedAsync();
            }
        }

        private async Task EnsureSharedCollectionsLoadedAsync()
        {
            if (sharedCollectionsLoaded)
                return;

            spinnerModal.ShowModal();
            _appState.sharedCollections = await WorkspaceService.GetSharedCollectionsAsync(_appState.userEmail);
            sharedCollections = _appState.sharedCollections;
            sharedCollectionsLoaded = true;
            await LoadCollectionOwnerNames();
            spinnerModal.CloseModal();
            StateHasChanged();
        }

        private async Task EnsureSharedCollectionsAvailableForActiveTabAsync()
        {
            if (activeCollectionTab == "shared")
            {
                await EnsureSharedCollectionsLoadedAsync();
            }
        }

        // Pagination methods for shared collections
        private void SharedCollectionsNextPage()
        {
            if (sharedCollectionsCurrentPage < SharedCollectionsTotalPages)
                sharedCollectionsCurrentPage++;
        }

        private void SharedCollectionsPreviousPage()
        {
            if (sharedCollectionsCurrentPage > 1)
                sharedCollectionsCurrentPage--;
        }

        private void SharedCollectionsFirstPage() => sharedCollectionsCurrentPage = 1;
        private void SharedCollectionsLastPage() => sharedCollectionsCurrentPage = SharedCollectionsTotalPages;

        // Pagination methods for snippets
        private void SnippetsNextPage()
        {
            if (snippetsCurrentPage < SnippetsTotalPages)
                snippetsCurrentPage++;
        }

        private void SnippetsPreviousPage()
        {
            if (snippetsCurrentPage > 1)
                snippetsCurrentPage--;
        }

        private void SnippetsFirstPage() => snippetsCurrentPage = 1;
        private void SnippetsLastPage() => snippetsCurrentPage = SnippetsTotalPages;

        #endregion

        #region Display Helpers

        private async Task LoadCollectionOwnerNames()
        {
            collectionOwnerNames = await WorkspaceService.GetCollectionOwnerNamesAsync(collections.Concat(sharedCollections));
        }

        private string GetCollectionOwnerDisplay(Collection collection)
        {
            if (collection == null || string.IsNullOrWhiteSpace(collection.CreatedBy))
                return "Unknown owner";

            return collectionOwnerNames.TryGetValue(collection.CreatedBy, out var ownerName)
                ? ownerName
                : collection.CreatedBy;
        }

        private string GetSelectedCollectionOwnerDisplay()
        {
            var selectedCollection = collections
                .Concat(sharedCollections)
                .FirstOrDefault(c => c.Id == selectedCollectionId);

            return selectedCollection == null
                ? "Unknown owner"
                : GetCollectionOwnerDisplay(selectedCollection);
        }

        #endregion
    }

}
