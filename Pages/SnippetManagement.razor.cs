using Microsoft.AspNetCore.Components;
using Snipster.Components;
using static Snipster.Data.DBContext;

namespace Snipster.Pages
{
    public partial class SnippetManagement
    {
        private string searchQuery = string.Empty;
        private List<Snippet> allSnippets = new List<Snippet>();
        private List<Snippet> filteredSnippets = new List<Snippet>();
        private Dictionary<string, IEnumerable<string>> relatedCollections = new Dictionary<string, IEnumerable<string>>();
        private Snippet newSnippet = new Snippet();
        private Modal createSnippetModal;
        private Modal editSnippetModal;
        private List<Collection> allCollections = new List<Collection>(); // List of all available collections
        private List<string> selectedCollectionIds = new List<string>(); // Stores selected collection IDs

        protected override async Task OnInitializedAsync()
        {
            // Load snippets and collections
            await LoadSnippets();
            await LoadCollections(); // Load all collections
        }

        private async Task LoadSnippets()
        {
            //filteredSnippets = await MongoDbService.GetAllSnippetsAsync();
            var loggedInUser = await SessionStorage.GetAsync<string>("userEmail");
            var loggedInUservalue = !string.IsNullOrEmpty(loggedInUser.Value) ? loggedInUser.Value.ToString() : "";
            filteredSnippets = await MongoDbService.GetSnippetsByUserAsync(!string.IsNullOrEmpty(loggedInUser.Value) ? loggedInUser.ToString() : "");
            await LoadRelatedCollections();
        }

        private async Task LoadCollections()
        {
            // Load all available collections
            allCollections = await MongoDbService.GetCollectionsAsync();
        }

        private void OnCollectionSelectionChanged(ChangeEventArgs e)
        {
            // Extract the selected values as an array
            var selectedOptions = e.Value as IEnumerable<string>;

            if (selectedOptions != null)
            {
                selectedCollectionIds = selectedOptions.ToList(); // Update the selectedCollectionIds list
            }
        }

        private async Task DeleteSnippet(string id)
        {
            await MongoDbService.DeleteSnippetAsync(id);
            await LoadSnippets();
        }

        private async Task<IEnumerable<string>> GetRelatedCollections(string snippetId)
        {
            var collections = await MongoDbService.GetCollectionsBySnippetId(snippetId);
            return collections.Select(c => c.Title);
        }

        private void ShowCreateSnippetModal() => createSnippetModal.ShowModal();  // Show the modal

        private async Task EditSnippet(Snippet snippet)
        {
            newSnippet = snippet;

            // Fetch the collections that the snippet is related to and extract their IDs
            var collections = await MongoDbService.GetCollectionsBySnippetId(snippet.Id);

            // Update the selectedCollectionIds with the collection IDs
            selectedCollectionIds = collections.Select(c => c.Id).ToList();  // Collect the IDs of the collections

            createSnippetModal.ShowModal();  // Show the modal in edit mode
        }

        private async Task LoadRelatedCollections()
        {
            // Load related collections for each snippet
            foreach (var snippet in filteredSnippets)
            {
                var collections = await GetRelatedCollections(snippet.Id);
                relatedCollections[snippet.Id] = collections;
            }
        }

        private async Task HandleValidSubmit()
        {
            foreach (var collectionId in selectedCollectionIds)
            {
                var collection = allCollections.FirstOrDefault(c => c.Id == collectionId);
                if (collection != null)
                {
                    // Initialize SnippetIds if it's null
                    collection.SnippetIds ??= new List<string>();

                    // Add the snippet ID to the collection's SnippetIds list
                    if (!collection.SnippetIds.Contains(newSnippet.Id))
                    {
                        collection.SnippetIds.Add(newSnippet.Id);
                    }
                }
            }
            // Save the snippet (add the snippet data and the updated collection references)
            await MongoDbService.SaveSnippetAsync(newSnippet);
            await MongoDbService.UpdateCollectionsAsync(allCollections); // Update collections to save SnippetIds
            await LoadSnippets();
            createSnippetModal.CloseModal();
        }

        private async Task HandleValidSubmitNew()
        {
            // Add the new snippet to the database
            await MongoDbService.AddSnippetAsync(newSnippet);

            // Update collections (only if there are collections selected)
            foreach (var collectionId in selectedCollectionIds)
            {
                var collection = allCollections.FirstOrDefault(c => c.Id == collectionId);
                if (collection != null)
                {
                    // Add the snippet ID to the collection's SnippetIds list
                    if (!collection.SnippetIds.Contains(newSnippet.Id))
                    {
                        collection.SnippetIds.Add(newSnippet.Id);
                    }
                }
            }

            // Save changes to collections
            await MongoDbService.UpdateCollectionsAsync(allCollections);

            // Reset the form and close the modal
            newSnippet = new Snippet();
            selectedCollectionIds.Clear();
            createSnippetModal.CloseModal();  // Close the modal after submission
            await LoadSnippets();
        }
    }
}
