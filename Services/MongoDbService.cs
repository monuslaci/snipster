using MongoDB.Driver;
using static Snipster.Data.DBContext;

namespace Snipster.Services
{
    public class MongoDbService
    {
        private readonly IMongoCollection<Snippet> _snippetsCollection;
        private readonly IMongoCollection<Collections> _collectionsCollection;
        private readonly IMongoCollection<Users> _usersCollection;

        public MongoDbService(string connectionString, string databaseName)
        {
            var client = new MongoClient(connectionString);
            var database = client.GetDatabase(databaseName);
            _snippetsCollection = database.GetCollection<Snippet>("Snippets");
            _collectionsCollection = database.GetCollection<Collections>("Collections");
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

        public async Task CreateCollectionAsync(Collections collection)
        {
            await _collectionsCollection.InsertOneAsync(collection);
        }

        public async Task<List<Collections>> GetCollectionsAsync()
        {
            return await _collectionsCollection.Find(_ => true).ToListAsync();
        }

        public async Task AddCollectionAsync(Collections collection)
        {
            await _collectionsCollection.InsertOneAsync(collection);
        }

        //public async Task CreateListAsync(Lists list)
        //{
        //    await _lists.InsertOneAsync(list);
        //}
    }
}
