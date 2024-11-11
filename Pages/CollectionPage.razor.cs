using Microsoft.AspNetCore.Components;
using Snipster.Services;
using System;
using System.Net.NetworkInformation;
using System.Reflection;
using Snipster.Components;
using static Snipster.Data.DBContext;


namespace Snipster.Pages
{
    public partial class CollectionPage
    {
        private List<Collection> collections = new List<Collection>();
        private Collection newCollection = new Collection();
        private Modal createCollectionModal;
        private List<Snippet> snippets = new List<Snippet>();
        private Snippet selectedSnippet;

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
            // Get the snippet IDs for the given collection ID
            var snippetIds = await _mongoDbService.GetSnippetIdsByCollectionAsync(collectionId);

            List<Snippet> snippets = new List<Snippet>();

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

            Console.WriteLine($"Loaded {snippets.Count} snippets for collection {collectionId}");

        }
        private async Task LoadSnippetDetails(string snippetId)
        {
            selectedSnippet = await _mongoDbService.GetSnippetByIdAsync(snippetId);
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
    }
}
