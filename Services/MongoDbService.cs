using MongoDB.Driver;
using static Snipster.Data.DBContext;

namespace Snipster.Services
{
    public class MongoDbService
    {
        private readonly IMongoCollection<Snippet> _snippetsCollection;
        private readonly IMongoCollection<Collection> _collectionsCollection;
        private readonly IMongoCollection<Users> _usersCollection;

        public MongoDbService(string connectionString, string databaseName)
        {
            var client = new MongoClient(connectionString);
            var database = client.GetDatabase(databaseName);
            _snippetsCollection = database.GetCollection<Snippet>("Snippets");
            _collectionsCollection = database.GetCollection<Collection>("Collections");
        }

        public async Task<string> AddSnippetAsync(Snippet snippet)
        {
            await _snippetsCollection.InsertOneAsync(snippet);
            return snippet.Id; 
        }

        public async Task<List<Snippet>> GetAllSnippetsAsync()
        {
            return await _snippetsCollection.Find(_ => true).ToListAsync();
        }

        public async Task<Snippet> GetSnippetByIdAsync(string id)
        {
            return await _snippetsCollection.Find(s => s.Id == id).FirstOrDefaultAsync();
        }

        public async Task<List<string>> GetSnippetIdsByCollectionAsync(string collectionId)
        {
            var collection = await _collectionsCollection.Find(c => c.Id == collectionId).FirstOrDefaultAsync();
            if (collection != null)
            {
                return collection.SnippetIds;
            }
            return new List<string>();
        }

        public async Task<List<Snippet>> GetSnippetsByCollectionAsync(string collectionId)
        {
            var collection = await _collectionsCollection.Find(c => c.Id == collectionId).FirstOrDefaultAsync();

            if (collection != null && collection.SnippetIds != null && collection.SnippetIds.Any())
            {
                var snippetIds = collection.SnippetIds;
                var snippets = await _snippetsCollection.Find(snippet => snippetIds.Contains(snippet.Id)).ToListAsync();
                return snippets;
            }
            return new List<Snippet>();
        }

        public async Task<List<Collection>> GetCollectionsBySnippetId(string snippetId)
        {
            var filter = Builders<Collection>.Filter.AnyIn(c => c.SnippetIds, new[] { snippetId });
            return await _collectionsCollection.Find(filter).ToListAsync();
        }

        public async Task SaveSnippetAsync(Snippet snippet)
        {
            // Insert or update snippet
            var existingSnippet = await _snippetsCollection.Find(s => s.Id == snippet.Id).FirstOrDefaultAsync();
            if (existingSnippet == null)
            {
                await _snippetsCollection.InsertOneAsync(snippet);  // Insert if new
            }
            else
            {
                var filter = Builders<Snippet>.Filter.Eq(s => s.Id, snippet.Id);
                var update = Builders<Snippet>.Update
                    .Set(s => s.Title, snippet.Title)
                    .Set(s => s.Content, snippet.Content);
                await _snippetsCollection.UpdateOneAsync(filter, update);  // Update if exists
            }
        }

        public async Task UpdateCollectionsAsync(List<Collection> collections)
        {
            foreach (var collection in collections)
            {
                // For each collection, we need to update the list of SnippetIds with the new snippet IDs
                var filter = Builders<Collection>.Filter.Eq(c => c.Id, collection.Id);
                var update = Builders<Collection>.Update.Set(c => c.SnippetIds, collection.SnippetIds);
                await _collectionsCollection.UpdateOneAsync(filter, update);
            }
        }

        public async Task UpdateCollectionAsync(Collection collection)
        {
                var filter = Builders<Collection>.Filter.Eq(c => c.Id, collection.Id);
                var update = Builders<Collection>.Update.Set(c => c.SnippetIds, collection.SnippetIds);
                await _collectionsCollection.UpdateOneAsync(filter, update);
        }

        public async Task UpdateSnippetAsync(Snippet snippet)
        {
            var filter = Builders<Snippet>.Filter.Eq(s => s.Id, snippet.Id);
            var update = Builders<Snippet>.Update
                .Set(s => s.Title, snippet.Title)
                .Set(s => s.Content, snippet.Content);

            await _snippetsCollection.UpdateOneAsync(filter, update);
        }

        public async Task DeleteSnippetAsync(string id)
        {
            await _snippetsCollection.DeleteOneAsync(s => s.Id == id);
        }

        public async Task CreateCollectionAsync(Collection collection)
        {
            await _collectionsCollection.InsertOneAsync(collection);
        }

        public async Task<List<Collection>> GetCollectionsAsync()
        {
            return await _collectionsCollection.Find(_ => true).ToListAsync();
        }

        public async Task AddCollectionAsync(Collection collection)
        {
            await _collectionsCollection.InsertOneAsync(collection);
        }

        //public async Task CreateListAsync(Lists list)
        //{
        //    await _lists.InsertOneAsync(list);
        //}
    }
}
