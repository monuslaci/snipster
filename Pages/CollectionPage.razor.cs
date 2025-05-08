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

namespace Snipster.Pages
{
    public partial class CollectionPage
    {
        [Inject] NavigationManager Navigation { get; set; }
        [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; }
        [Inject] MongoDbService _mongoDbService { get; set; }

        private List<Collection> collections = new List<Collection>();
        private List<Collection> sharedCollections = new List<Collection>();
        private Collection newCollection = new Collection();
        private Collection editCollection = new Collection();
        private Snippet newSnippet = new Snippet();
        private List<Snippet> snippets = new List<Snippet>();
        private Snippet selectedSnippet { get; set; }
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
                var authState = await AuthStateProvider.GetAuthenticationStateAsync();
                userEmail = authState.User.Identity?.Name;

                user = await _mongoDbService.GetUser(userEmail);

                spinnerModal.IsSpinner = true;
                spinnerModal.ShowModal();

                //get the user's collections
                collections = await _mongoDbService.GetCollectionsForUserAsync(userEmail);
                //get the collections that are shared with them
                sharedCollections = await _mongoDbService.GetSharedCollectionsForUserAsync(userEmail);

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
            collections = await _mongoDbService.GetCollectionsAsync(); 
            createCollectionModal.CloseModal();
            spinnerModal.CloseModal();
        }

        private async Task LoadSnippets(string collectionId)
        {        
            spinnerModal.ShowModal();
            selectedCollectionId = collectionId;
            // Get the snippet IDs for the selected collection
            var snippetIds = await _mongoDbService.GetSnippetIdsByCollectionAsync(selectedCollectionId);

            // Clear the current snippets list and populate it with the new ones
            snippets.Clear();

            if (snippetIds != null)
            {
                foreach (var snippetId in snippetIds)
                {
                    var snippet = await _mongoDbService.GetSnippetByIdAsync(snippetId);
                    if (snippet != null)
                    {
                        snippets.Add(snippet);
                    }
                }
            }
            selectedCollectionIdCreate = selectedCollectionId;
            string firstSnippetId = snippetIds != null ? snippetIds.FirstOrDefault() : "";
            await LoadSnippetDetails(firstSnippetId);


            StateHasChanged();
            spinnerModal.CloseModal();
        }
  
        private async Task LoadSnippetDetails(string snippetId)
        {
            spinnerModal.ShowModal();
            isAddingSnippet = false;
            selectedSnippet = await _mongoDbService.GetSnippetByIdAsync(snippetId);
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
            collections = await _mongoDbService.GetCollectionsAsync();
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
            collections = await _mongoDbService.GetCollectionsAsync(); // Refresh the list
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
            await _mongoDbService.SaveSnippetAsync(newSnippet);

            //add collectionId to user's my collection IDs
            user.MyCollectionIds.Add(selectedCollectionId);
            await _mongoDbService.UpdateUser(user);

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
                        await _mongoDbService.UpdateUser(user);
                    }

                }
            }


            isAddingSnippet = false;
            await LoadSnippets(selectedCollectionId);
            spinnerModal.CloseModal();
        }

        private async Task DeleteSnippet(string id)
        {
            spinnerModal.ShowModal();
            await _mongoDbService.DeleteSnippetAsync(id);

            //delete the snippet id from the users' SharedSnippetIds
            var snippet = await _mongoDbService.GetSnippetByIdAsync(id);
            if (snippet.SharedWith.Any())
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

            //reload the collection without the snippet
            await LoadSnippets(selectedCollectionId);

            var snippetIds = await _mongoDbService.GetSnippetIdsByCollectionAsync(selectedCollectionId);
            var snippetid = snippetIds.FirstOrDefault();

            await LoadSnippetDetails(snippetid);
            spinnerModal.CloseModal();
        }

        private async Task OnSearchCollection()
        {
            if (!string.IsNullOrEmpty(searchCollectionQuery))
            {
                collections = await _mongoDbService.SearchCollectionAsync(searchCollectionQuery, userEmail);

                if (collections != null)
                {
                    selectedCollectionId = collections.FirstOrDefault().Id;
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

            collections = await _mongoDbService.GetCollectionsForUserAsync(userEmail);

            if (collections != null)
            {
                selectedCollectionId = collections.FirstOrDefault().Id;
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
