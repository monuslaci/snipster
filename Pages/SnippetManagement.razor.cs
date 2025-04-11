using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Snipster.Components;
using Snipster.Services;
using static Snipster.Data.DBContext;

namespace Snipster.Pages
{
    public partial class SnippetManagement
    {
        [Inject] MongoDbService MongoDbService { get; set; }
        [Inject] NavigationManager Navigation { get; set; }
        [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; }
        private string searchQuery { get; set; }
        private List<Snippet> filteredSnippets = new List<Snippet>();
        private Dictionary<string, IEnumerable<string>> relatedCollections = new Dictionary<string, IEnumerable<string>>();
        private Snippet newSnippet = new Snippet();
        private Modal createSnippetModal { get; set; }
        private Modal editSnippetModal { get; set; }
        private List<Collection> allCollections = new List<Collection>(); // List of all available collections
        private List<string> selectedCollectionIds = new List<string>(); // Stores selected collection IDs
        private string searchSnippetQuery { get; set; }
        private Modal spinnerModal { get; set; }
        private Snippet selectedSnippet { get; set; }
        private string userEmail { get; set; }
        private bool IsFavouriteSearch { get; set; }

        protected override async Task OnInitializedAsync()
        {
            //loggedInUser = await SessionStorage.GetAsync<string>("userEmail");
            //var loggedInUserEmail = !string.IsNullOrEmpty(loggedInUser.Value) ? loggedInUser.Value.ToString() : "";

     
        }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                var authState = await AuthStateProvider.GetAuthenticationStateAsync();
                userEmail = authState.User.Identity?.Name;

                spinnerModal.IsSpinner = true;
                spinnerModal.ShowModal();
                await LoadSnippets();
                await LoadCollections(); // Load all collections
                StateHasChanged();
                spinnerModal.CloseModal();
            }

        }
        private async Task LoadSnippets()
        {
            //filteredSnippets = await MongoDbService.GetAllSnippetsAsync();

            filteredSnippets = await MongoDbService.GetSnippetsByUserAsync(userEmail);
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
        private async Task OpenSnippet(string snippetId)
        {
            selectedSnippet = await MongoDbService.GetSnippetByIdAsync(snippetId);
            var collection = await MongoDbService.GetCollectionsBySnippetId(selectedSnippet.Id);
            var selectedCollectionId = collection != null && collection.Count()>0 ? collection.FirstOrDefault().Id : "";
            Navigation.NavigateTo($"/collections?selectedCollectionId={selectedCollectionId}&selectedSnippetId={selectedSnippet.Id}");
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

        private async Task SearchFavouriteSnippets()
        {
            IsFavouriteSearch = !IsFavouriteSearch;
            await OnSearchSnippet();
        }

        private async Task OnSearchSnippet()
        {
            spinnerModal.ShowModal();

            // Instead of clearing first, directly assign the new filtered list
            var results = await MongoDbService.SearchSnippetAsync(searchSnippetQuery, userEmail, IsFavouriteSearch);

            // Assign new results directly
            filteredSnippets = results;

            StateHasChanged();
            spinnerModal.CloseModal();
        }
        private async Task CancelSearchSnippet()
        {
            filteredSnippets = await MongoDbService.GetSnippetsByUserAsync(!string.IsNullOrEmpty(userEmail) ? userEmail : "");

            searchSnippetQuery = "";
            IsFavouriteSearch = false;
            StateHasChanged();
        }
    }
}
