

using static Snipster.Data.DBContext;

namespace Snipster.Pages
{
    public partial class CreateSnippet
    {
        private Snippet newSnippet = new Snippet();

        private async Task HandleValidSubmit()
        {
            await MongoDbService.AddSnippetAsync(newSnippet);
            newSnippet = new Snippet(); // Clear form
        }

    }
}
