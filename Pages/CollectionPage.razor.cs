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

namespace Snipster.Pages
{
    public partial class CollectionPage
    {
        [Inject] NavigationManager Navigation { get; set; }
        [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; }
        private List<Collection> collections = new List<Collection>();
        private Collection newCollection = new Collection();
        private Collection editCollection = new Collection();
        private Snippet newSnippet = new Snippet();
        private List<Snippet> snippets = new List<Snippet>();
        private Snippet selectedSnippet;
        private bool isAddingSnippet = false; 
        private string selectedCollectionId; 
        private string selectedCollectionIdCreate; 
        private string selectedCollectionIdEdit; 
        private Modal createCollectionModal;
        private Modal editCollectionModal;
        private Modal spinnerModal = new Modal();
        private bool adjustHeightNeeded;
        private string HashtagsInput { get; set; }
        private List<string> ValidHashtags { get; set; } = new List<string>();
        private string userEmail { get; set; }

        private bool isFormValid = true;
        private bool isLeftPanelOpen = true;
        private bool isMiddlePanelOpen = true;
        private bool isRightPanelOpen = true;

        private string selectedSnippetDetails = "Select a snippet to see details.";
        private string rightPanelWidth = "50%";

        protected override async Task OnInitializedAsync()
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            userEmail = authState.User.Identity?.Name;

            collections = await _mongoDbService.GetCollectionsForUserAsync(userEmail);

            var uri = Navigation.ToAbsoluteUri(Navigation.Uri);
            var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);

            if (query != null && query.TryGetValue("selectedCollectionId", out var id))
            {
                selectedCollectionId = id;
                await LoadSnippets(selectedCollectionId);

                // Remove query parameter from the URL using JavaScript
                await JSRuntime.InvokeVoidAsync("updateUrlWithoutQueryParam", "/collections");
            }

        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                spinnerModal.IsSpinner = true;
                spinnerModal.ShowModal();

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

        private async Task HandleValidSubmitNew()
        {
            spinnerModal.ShowModal();
            // Save the new snippet and get the generated snippet ID
            var snippetId = await _mongoDbService.AddSnippetAsync(newSnippet);
            if (selectedCollectionIdCreate == null)
                selectedCollectionIdCreate = selectedCollectionId;
            var selectedCollection = collections.FirstOrDefault(c => c.Id == selectedCollectionIdCreate);
            if (selectedCollection != null)
            {
                // Add the new snippet ID to the collection's SnippetIds list
                selectedCollection.SnippetIds.Add(snippetId);
                selectedCollection.LastModifiedDate = DateTime.Now;
                await _mongoDbService.UpdateCollectionAsync(selectedCollection);
            }
            newSnippet.CreatedDate = DateTime.Now;
            newSnippet.LastModifiedDate = DateTime.Now;
            await _mongoDbService.SaveSnippetAsync(newSnippet);
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
            isAddingSnippet = false;
            await LoadSnippets(selectedCollectionId);
            spinnerModal.CloseModal();
        }

        private async Task DeleteSnippet(string id)
        {
            spinnerModal.ShowModal();
            await _mongoDbService.DeleteSnippetAsync(id);
            await LoadSnippets(selectedCollectionId);

            var snippetIds = await _mongoDbService.GetSnippetIdsByCollectionAsync(selectedCollectionId);
            var snippetid = snippetIds.FirstOrDefault();

            await LoadSnippetDetails(snippetid);
            spinnerModal.CloseModal();
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
