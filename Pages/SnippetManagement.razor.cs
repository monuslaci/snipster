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

        protected override async Task OnInitializedAsync()
        {
            await LoadSnippets();
        }

        private async Task LoadSnippets()
        {
            filteredSnippets = await MongoDbService.GetAllSnippetsAsync();
            await LoadRelatedCollections();
        }

        private async Task HandleValidSubmit()
        {
            await MongoDbService.AddSnippetAsync(newSnippet);
            newSnippet = new Snippet();
            createSnippetModal.CloseModal();  // Close the modal after submission
            await LoadSnippets();
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

        private void EditSnippet(Snippet snippet)
        {
            newSnippet = snippet;
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
    }
}
