using Microsoft.AspNetCore.Components;
using Snipster.Services;
using System;
using System.Net.NetworkInformation;
using System.Reflection;
using Snipster.Components;
using static Snipster.Data.DBContext;
using Microsoft.JSInterop;

namespace Snipster.Pages
{
    public partial class CollectionPage
    {
        private List<Collection> collections = new List<Collection>();
        private Collection newCollection = new Collection();
        private Snippet newSnippet = new Snippet();
        private List<Snippet> snippets = new List<Snippet>();
        private Snippet selectedSnippet;
        private bool isAddingSnippet = false; // Control visibility of snippet creation fields
        private string selectedCollectionId; // Store selected collection ID
        private Modal createCollectionModal;


        protected override async Task OnInitializedAsync()
        {
            collections = await _mongoDbService.GetCollectionsAsync();
        }

        private void ShowCreateCollectionModal()
        {
            newCollection = new Collection(); // Reset the form model
            createCollectionModal.ShowModal();
        }

        private async Task HandleCreateCollection()
        {
            await _mongoDbService.AddCollectionAsync(newCollection);
            collections = await _mongoDbService.GetCollectionsAsync(); // Refresh the collection list
            createCollectionModal.CloseModal();
        }

        private async Task LoadSnippets(string collectionId)
        {
            // Get the snippet IDs for the selected collection
            var snippetIds = await _mongoDbService.GetSnippetIdsByCollectionAsync(collectionId);

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

            // Trigger a UI refresh
            StateHasChanged();
        }
  
        private async Task LoadSnippetDetails(string snippetId)
        {
            selectedSnippet = await _mongoDbService.GetSnippetByIdAsync(snippetId);
        }
        private void ShowSnippetFields()
        {
            // Reset the form model and show the new snippet fields
            newSnippet = new Snippet();
            isAddingSnippet = true;
        }

        private async Task EditCollection(string collectionId)
        {
            // Logic to edit collection (implement this as needed)
        }

        private async Task DeleteCollection(string collectionId)
        {
            // Logic to delete collection (implement this as needed)
            collections = await _mongoDbService.GetCollectionsAsync(); // Refresh the list
        }

        private async Task HandleValidSubmitNew()
        {
            // Save the new snippet and get the generated snippet ID
            var snippetId = await _mongoDbService.AddSnippetAsync(newSnippet);

            // Update the selected collection to include this new snippet ID
            var selectedCollection = collections.FirstOrDefault(c => c.Id == selectedCollectionId);
            if (selectedCollection != null)
            {
                // Add the new snippet ID to the collection's SnippetIds list
                selectedCollection.SnippetIds.Add(snippetId);

                // Update the collection in the database
                await _mongoDbService.UpdateCollectionAsync(selectedCollection);
            }

            // Refresh the snippets list for the selected collection
            snippets = await _mongoDbService.GetSnippetsByCollectionAsync(selectedCollectionId);

            // Reset the form
            newSnippet = new Snippet();
            isAddingSnippet = false;
        }

        private async Task AdjustTextAreaHeight(ChangeEventArgs args)
        {
            await JSRuntime.InvokeVoidAsync("adjustTextAreaHeight", "createContentId");
        }


    }
}
