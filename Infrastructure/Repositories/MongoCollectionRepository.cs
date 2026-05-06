using MongoDB.Driver;
using Snipster.Application.Workspace.Repositories;
using static Snipster.Data.DBContext;

namespace Snipster.Infrastructure.Repositories;

public class MongoCollectionRepository : ICollectionRepository
{
    private readonly IMongoCollection<Collection> _collectionsCollection;
    private readonly IMongoCollection<Snippet> _snippetsCollection;
    private readonly IMongoCollection<Users> _usersCollection;

    public MongoCollectionRepository(IMongoDatabase database)
    {
        _collectionsCollection = database.GetCollection<Collection>("Collections");
        _snippetsCollection = database.GetCollection<Snippet>("Snippets");
        _usersCollection = database.GetCollection<Users>("Users");
    }

    public async Task<List<Collection>> GetForUserAsync(string email)
    {
        var user = await _usersCollection.Find(u => u.Email == email).FirstOrDefaultAsync();
        if (user == null || user.MyCollectionIds == null || !user.MyCollectionIds.Any())
            return new List<Collection>();

        var filter = Builders<Collection>.Filter.In(c => c.Id, user.MyCollectionIds);
        return await _collectionsCollection.Find(filter).ToListAsync();
    }

    public async Task<List<Collection>> GetSharedForUserAsync(string email)
    {
        var user = await _usersCollection.Find(u => u.Email == email).FirstOrDefaultAsync();
        if (user == null || user.SharedSnippetIds == null || !user.SharedSnippetIds.Any())
            return new List<Collection>();

        var snippetFilter = Builders<Snippet>.Filter.In(s => s.Id, user.SharedSnippetIds);
        var sharedSnippets = await _snippetsCollection.Find(snippetFilter).ToListAsync();

        var sharedCollectionIds = sharedSnippets
            .Where(s => !string.IsNullOrEmpty(s.CollectionId))
            .Select(s => s.CollectionId)
            .Distinct()
            .ToList();

        if (!sharedCollectionIds.Any())
            return new List<Collection>();

        var collectionFilter = Builders<Collection>.Filter.In(c => c.Id, sharedCollectionIds);
        return await _collectionsCollection.Find(collectionFilter).ToListAsync();
    }

    public Task AddAsync(Collection collection)
    {
        return _collectionsCollection.InsertOneAsync(collection);
    }

    public async Task UpdateAsync(Collection collection)
    {
        var filter = Builders<Collection>.Filter.Eq(c => c.Id, collection.Id);
        var update = Builders<Collection>.Update
            .Set(c => c.SnippetIds, collection.SnippetIds)
            .Set(c => c.Title, collection.Title)
            .Set(c => c.IsPublic, collection.IsPublic)
            .Set(c => c.CreatedBy, collection.CreatedBy)
            .Set(c => c.LastModifiedDate, collection.LastModifiedDate);

        await _collectionsCollection.UpdateOneAsync(filter, update);
    }

    public async Task DeleteAsync(string collectionId)
    {
        var collection = await _collectionsCollection
            .Find(c => c.Id == collectionId)
            .FirstOrDefaultAsync();

        if (collection == null)
            return;

        var snippetIds = collection.SnippetIds;
        await _collectionsCollection.DeleteOneAsync(c => c.Id == collectionId);

        var otherCollections = await _collectionsCollection
            .Find(Builders<Collection>.Filter.Ne(c => c.Id, collectionId))
            .ToListAsync();

        var stillReferencedSnippetIds = otherCollections
            .SelectMany(c => c.SnippetIds)
            .ToHashSet();

        var orphanedSnippetIds = snippetIds
            .Where(id => !stillReferencedSnippetIds.Contains(id))
            .ToList();

        if (orphanedSnippetIds.Any())
        {
            var snippetDeleteFilter = Builders<Snippet>.Filter.In(s => s.Id, orphanedSnippetIds);
            await _snippetsCollection.DeleteManyAsync(snippetDeleteFilter);
        }
    }

    public Task<List<Collection>> GetBySnippetIdAsync(string snippetId)
    {
        var filter = Builders<Collection>.Filter.AnyIn(c => c.SnippetIds, new[] { snippetId });
        return _collectionsCollection.Find(filter).ToListAsync();
    }
}
