using MongoDB.Driver;
using static Snipster.Data.DBContext;

namespace Snipster.Services
{
    public class MongoDbService
    {
        private readonly IMongoCollection<Snippet> _snippetsCollection;

        public MongoDbService(string connectionString, string databaseName)
        {
            var client = new MongoClient(connectionString);
            var database = client.GetDatabase(databaseName);
            _snippetsCollection = database.GetCollection<Snippet>("Snippets");
        }

        public async Task AddSnippetAsync(Snippet snippet)
        {
            await _snippetsCollection.InsertOneAsync(snippet);
        }

        public async Task<List<Snippet>> GetAllSnippetsAsync()
        {
            return await _snippetsCollection.Find(_ => true).ToListAsync();
        }

        public async Task<Snippet> GetSnippetByIdAsync(string id)
        {
            return await _snippetsCollection.Find(s => s.Id == id).FirstOrDefaultAsync();
        }

        public async Task UpdateSnippetAsync(string id, Snippet updatedSnippet)
        {
            await _snippetsCollection.ReplaceOneAsync(s => s.Id == id, updatedSnippet);
        }

        public async Task DeleteSnippetAsync(string id)
        {
            await _snippetsCollection.DeleteOneAsync(s => s.Id == id);
        }
    }
}
