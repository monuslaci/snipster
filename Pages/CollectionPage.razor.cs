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
using static Microsoft.Extensions.Logging.EventSource.LoggingEventSource;
using System.Collections.Generic;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Microsoft.AspNetCore.Components.Web;
using static Snipster.Data.CommonClasses;
using Snipster.Services.AppStates;
using Snipster.Helpers;

namespace Snipster.Pages
{
    public partial class CollectionPage
    {
        [Inject] NavigationManager Navigation { get; set; }
        [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; }
        [Inject] MongoDbService _mongoDbService { get; set; }
        [Inject] private AppState _appState { get; set; }
        [Inject] private IGeneralHelpers _helper { get; set; }
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
        private string? searchCollectionQuery { get; set; }
        private string? searchSnippetQuery { get; set; }
        private bool isFormValid { get; set; } = true;
        private bool isLeftPanelOpen { get; set; } = true;
        private bool isMiddlePanelOpen { get; set; } = true;
        private string rightPanelWidth { get; set; } = "50%";
        private bool IsFavouriteSearch { get; set; }
        private bool selectedCollectionIsOwn { get; set; }
        private string isDisabled { get; set; } = "";

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


                if (!_appState.collections.Any() )
                    await _appState.LoadCollections();
                if (!_appState.sharedCollections.Any())
                    await _appState.LoadSharedCollections();

                //get the user's collections
                collections = _appState.collections;
                //get the collections that are shared with them
                sharedCollections = _appState.sharedCollections;

                if (!string.IsNullOrEmpty(selectedCollectionId))
                    await LoadSnippets(selectedCollectionId);

                if (collections != null && !string.IsNullOrEmpty(selectedSnippetId))
                {
                    await LoadSnippetDetails(selectedSnippetId);
                }

                // Remove query parameter from the URL using JavaScript
                await JSRuntime.InvokeVoidAsync("updateUrlWithoutQueryParam", "/collections");
                StateHasChanged();
                spinnerModal.CloseModal();
            }
            if (adjustHeightNeeded)
            {
                adjustHeightNeeded = false; 
                await AdjustTextAreaHeightEdit();
            }
        }

        #region Create Collection
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


            newCollection.LastModifiedDate = DateTime.Now;
            await _mongoDbService.AddCollectionAsync(newCollection);

            _appState.collections.Add(newCollection);

            _appState.collections = _appState.collections
                .OrderBy(c => c.LastModifiedDate)
                .ToList();

            ////get the user's collections
            //await _appState.LoadCollections();

            collections = _appState.collections;

            //get the collections that are shared with them
            sharedCollections = _appState.sharedCollections;

            //add collectionId to user's my collection IDs
            user.MyCollectionIds.Add(newCollection.Id);
            await _mongoDbService.UpdateUser(user);

            StateHasChanged();

            createCollectionModal.CloseModal();
            spinnerModal.CloseModal();
        }
        #endregion

        private async Task LoadSnippets(string collectionId)
        {        
            spinnerModal.ShowModal();
            selectedCollectionId = collectionId;

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


            foreach (var ls in _appState.loadedSnippets) 
            {
                if (ls.collectionId == selectedCollectionId)
                    snippets.AddRange(ls.snippetList); 
            }
            foreach (var lss in _appState.loadedSharedSnippets)
            {
                if (lss.collectionId == selectedCollectionId)
                    snippets.AddRange(lss.snippetList);
            }

            selectedCollectionIdCreate = selectedCollectionId;
            string firstSnippetId = snippets != null && snippets.Count > 0 ? snippets.FirstOrDefault().Id : "";
            await LoadSnippetDetails(firstSnippetId);


            StateHasChanged();
            spinnerModal.CloseModal();
        }
  
        private async Task LoadSnippetDetails(string snippetId)
        {
            spinnerModal.ShowModal();
            isAddingSnippet = false;
            //selectedSnippet = await _mongoDbService.GetSnippetByIdAsync(snippetId);
            selectedSnippet = snippets == null ? new Snippet() : snippets.Where(s => s.Id == snippetId).FirstOrDefault();


            if (selectedSnippet != null) 
                selectedSnippetOriginalSharedWith = selectedSnippet.SharedWith;


            adjustHeightNeeded = true;
            spinnerModal.CloseModal();

        }
        private void ShowSnippetFields()
        {
            // Reset the form model and show the new snippet fields
            newSnippet = new Snippet();
            selectedSnippet = new Snippet();
            isAddingSnippet = true;
        }


        private async Task HandleEditCollection()
        {
            ValidHashtags.Clear();
            spinnerModal.ShowModal();
            editCollection.LastModifiedDate = DateTime.Now;
            editCollection.CreatedBy = userEmail;
            await _mongoDbService.UpdateCollectionAsync(editCollection);
            editCollectionModal.CloseModal();

            //update the collection in the appstate collection list
            await _helper.UpdateCollectionInMemory(editCollection);

            //get the user's collections
            collections = _appState.collections;
            //get the collections that are shared with them
            sharedCollections = _appState.sharedCollections;

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
            await _mongoDbService.DeleteCollectionAsync(collectionId);

            await _helper.DeleteCollectionFromMemory(collectionId);

            //get the user's collections
            collections = _appState.collections;

            spinnerModal.CloseModal();
        }

        private async Task EditSnippetFavourite(Snippet snippet)
        {
            snippet.IsFavourite = !snippet.IsFavourite;
            await _mongoDbService.SaveSnippetAsync(snippet);
        }
        private async Task SearchFavouriteSnippets()
        {
            IsFavouriteSearch = !IsFavouriteSearch;
            await OnSearchSnippet();
        }
        
        private async Task HandleValidSubmitNew() //new snippet
        {
            spinnerModal.ShowModal();

            // Save the new snippet and get the generated snippet ID
            var snippetId = await _mongoDbService.AddSnippetAsync(newSnippet);
            if (selectedCollectionIdCreate == null)
                selectedCollectionIdCreate = selectedCollectionId;
            var selectedCollection = collections.FirstOrDefault(c => c.Id == selectedCollectionIdCreate);


            //update collection
            if (selectedCollection != null)
            {
                // Add the new snippet ID to the collection's SnippetIds list
                selectedCollection.SnippetIds.Add(snippetId);
                selectedCollection.LastModifiedDate = DateTime.Now;
                await _mongoDbService.UpdateCollectionAsync(selectedCollection);
            }

            //update snippet
            newSnippet.CreatedDate = DateTime.Now;
            newSnippet.LastModifiedDate = DateTime.Now;
            newSnippet.CollectionId = selectedCollectionId;
            newSnippet.CreatedBy = user.Id;
            await _mongoDbService.SaveSnippetAsync(newSnippet);



            //add the snippet id to the users' SharedSnippetIds
            if (newSnippet.SharedWith.Any())
            {
                foreach (var sharedEmail in newSnippet.SharedWith)
                {
                    var modifiedUser = await _mongoDbService.GetUser(sharedEmail);
                    if (modifiedUser != null)
                    {
                        modifiedUser.SharedSnippetIds.Add(snippetId);
                        await _mongoDbService.UpdateUser(user);
                    }

                }
            }          

            isAddingSnippet = false;

            await _helper.AddNewSnippetInMemory(selectedCollectionIdCreate, newSnippet, snippetId);

            await LoadSnippets(selectedCollectionId);

            selectedSnippet = newSnippet;
            adjustHeightNeeded = true;
            spinnerModal.CloseModal();
        }
        private async Task HandleValidSubmitEdit()
        {
            spinnerModal.ShowModal();
            var snippetId = selectedSnippet.Id;

            var selectedCollection = collections.FirstOrDefault(c => c.Id == selectedCollectionId);
            if (selectedCollection != null)
            {
                if (!selectedCollection.SnippetIds.Contains(selectedSnippet.Id))
                {
                    selectedCollection.SnippetIds.Add(snippetId);
                    selectedCollection.LastModifiedDate = DateTime.Now;
                    await _mongoDbService.UpdateCollectionAsync(selectedCollection);
                }

            }
            selectedSnippet.LastModifiedDate = DateTime.Now;
            await _mongoDbService.SaveSnippetAsync(selectedSnippet);

            //add the snippet id to the users' SharedSnippetIds
            if (selectedSnippet.SharedWith.Any())
            {
                foreach (var sharedEmail in selectedSnippet.SharedWith)
                {
                    var modifiedUser = await _mongoDbService.GetUser(sharedEmail);
                    if (modifiedUser != null)
                    {
                        modifiedUser.SharedSnippetIds.Add(selectedSnippet.Id);
                        await _mongoDbService.UpdateUser(modifiedUser);
                    }

                }
            }

            //remove if email address was removed from the shared text
            if (selectedSnippetOriginalSharedWith.Any())
            {
                foreach (var sn in selectedSnippetOriginalSharedWith)
                {
                    if (!selectedSnippet.SharedWith.Contains(sn))
                    {
                        var modifiedUser = await _mongoDbService.GetUser(sn);
                        if (modifiedUser != null)
                        {
                            modifiedUser.SharedSnippetIds.Remove(selectedSnippet.Id);
                            await _mongoDbService.UpdateUser(modifiedUser);
                        }
                    }
                }
            }


            isAddingSnippet = false;

            await _helper.UpdateSnippetInMemory(selectedCollectionId, selectedSnippet);
                        
            await LoadSnippets(selectedCollectionId);
            spinnerModal.CloseModal();
        }

        private async Task DeleteSnippet(string id)
        {
            spinnerModal.ShowModal();


            //delete the snippet id from the users' SharedSnippetIds
            var snippet = await _mongoDbService.GetSnippetByIdAsync(id);
            if (snippet != null && snippet.SharedWith.Any())
            {
                foreach (var sharedEmail in selectedSnippet.SharedWith)
                {
                    var modifiedUser = await _mongoDbService.GetUser(sharedEmail);
                    if (modifiedUser != null)
                    {
                        modifiedUser.SharedSnippetIds.Remove(selectedSnippet.Id);
                        await _mongoDbService.UpdateUser(user);
                    }
                }
            }

            await _mongoDbService.DeleteSnippetAsync(id);

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

        private async Task OnSearchCollection()
        {
            if (!string.IsNullOrEmpty(searchCollectionQuery))
            {
                collections = await _helper.SearchCollectionAsync(searchCollectionQuery, userEmail, _appState.collections);
                sharedCollections = await _helper.SearchCollectionAsync(searchCollectionQuery, userEmail, _appState.sharedCollections);

                if (collections.Any())
                {
                    selectedCollectionId = collections.FirstOrDefault().Id;
                    await LoadSnippets(selectedCollectionId);
                }
                else if (!collections.Any() && sharedCollections.Any())
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
                //get the list of filtered snippets
                List<Snippet> filteredSnippetlist = await _mongoDbService.SearchSnippetInSelectedCollectionAsync(searchSnippetQuery, selectedCollectionId, IsFavouriteSearch);

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

            collections = _appState.collections;
            sharedCollections = _appState.sharedCollections;

            if (collections.Any())
            {
                selectedCollectionId = collections.FirstOrDefault().Id;
                await LoadSnippets(selectedCollectionId);
            }
            else if (!collections.Any() && sharedCollections.Any())
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

        private async Task AdjustTextAreaHeight(ChangeEventArgs args)
        {
            await JSRuntime.InvokeVoidAsync("adjustTextAreaHeight", "createContentId");
        }
        private async Task AdjustTextAreaHeightEdit()
        {
            await JSRuntime.InvokeVoidAsync("adjustTextAreaHeight", "editContentId");
        }

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
    }

}
