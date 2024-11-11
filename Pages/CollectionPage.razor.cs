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
        private List<Collections> collections = new List<Collections>();
        private Collections newCollection = new Collections();
        private Modal createCollectionModal;

        protected override async Task OnInitializedAsync()
        {
            collections = await _mongoDbService.GetCollectionsAsync();
        }

        private void ShowCreateCollectionModal()
        {
            newCollection = new Collections(); // Reset the form model
            createCollectionModal.ShowModal();
        }

        private async Task HandleCreateCollection()
        {
            await _mongoDbService.AddCollectionAsync(newCollection);
            collections = await _mongoDbService.GetCollectionsAsync(); // Refresh the collection list
            createCollectionModal.CloseModal();
        }

    }
}
