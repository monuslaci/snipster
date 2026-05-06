using MongoDB.Bson;
using MongoDB.Driver;
using Snipster.Application.Workspace.Repositories;
using static Snipster.Data.DBContext;

namespace Snipster.Infrastructure.Repositories;

public class MongoSnippetRepository : ISnippetRepository
{
    private readonly IMongoCollection<Collection> _collectionsCollection;
    private readonly IMongoCollection<Snippet> _snippetsCollection;

    public MongoSnippetRepository(IMongoDatabase database)
    {
        _collectionsCollection = database.GetCollection<Collection>("Collections");
        _snippetsCollection = database.GetCollection<Snippet>("Snippets");
    }

    public async Task<string> AddAsync(Snippet snippet)
    {
        await _snippetsCollection.InsertOneAsync(snippet);
        return snippet.Id;
    }

    public Task<Snippet> GetByIdAsync(string snippetId)
    {
        return _snippetsCollection.Find(s => s.Id == snippetId).FirstOrDefaultAsync();
    }

    public async Task SaveAsync(Snippet snippet)
    {
        var existingSnippet = await _snippetsCollection.Find(s => s.Id == snippet.Id).FirstOrDefaultAsync();
        if (existingSnippet == null)
        {
            await _snippetsCollection.InsertOneAsync(snippet);
            return;
        }

        var filter = Builders<Snippet>.Filter.Eq(s => s.Id, snippet.Id);
        var update = Builders<Snippet>.Update
            .Set(s => s.Title, snippet.Title)
            .Set(s => s.Content, snippet.Content)
            .Set(s => s.HashtagsInput, snippet.HashtagsInput)
            .Set(s => s.LastModifiedDate, snippet.LastModifiedDate)
            .Set(s => s.IsFavourite, snippet.IsFavourite)
            .Set(s => s.SharedWithInput, snippet.SharedWithInput)
            .Set(s => s.CreatedBy, snippet.CreatedBy)
            .Set(s => s.CreatedDate, snippet.CreatedDate)
            .Set(s => s.CollectionId, snippet.CollectionId)
            .Set(s => s.Language, snippet.Language);

        await _snippetsCollection.UpdateOneAsync(filter, update);
    }

    public Task DeleteAsync(string snippetId)
    {
        return _snippetsCollection.DeleteOneAsync(s => s.Id == snippetId);
    }

    public async Task<List<Snippet>> GetByCollectionAsync(string collectionId)
    {
        var collection = await _collectionsCollection.Find(c => c.Id == collectionId).FirstOrDefaultAsync();
        if (collection == null || collection.SnippetIds == null || !collection.SnippetIds.Any())
            return new List<Snippet>();

        return await _snippetsCollection.Find(snippet => collection.SnippetIds.Contains(snippet.Id)).ToListAsync();
    }

    public async Task<List<Snippet>> GetSharedByCollectionForUserAsync(string collectionId, Users user)
    {
        if (string.IsNullOrEmpty(collectionId) || user == null || user.SharedSnippetIds == null || !user.SharedSnippetIds.Any())
            return new List<Snippet>();

        var collection = await _collectionsCollection.Find(c => c.Id == collectionId).FirstOrDefaultAsync();
        if (collection == null || collection.SnippetIds == null || !collection.SnippetIds.Any())
            return new List<Snippet>();

        var allowedSnippetIds = collection.SnippetIds
            .Where(id => user.SharedSnippetIds.Contains(id))
            .Distinct()
            .ToList();

        if (!allowedSnippetIds.Any())
            return new List<Snippet>();

        var filter = Builders<Snippet>.Filter.In(s => s.Id, allowedSnippetIds);
        return await _snippetsCollection.Find(filter).ToListAsync();
    }

    public async Task<List<Snippet>> SearchInCollectionAsync(string keyword, string? collectionId, bool isFavourite)
    {
        if (string.IsNullOrEmpty(collectionId))
            return new List<Snippet>();

        var collection = await _collectionsCollection.Find(c => c.Id == collectionId).FirstOrDefaultAsync();
        if (collection == null || collection.SnippetIds == null || !collection.SnippetIds.Any())
            return new List<Snippet>();

        var filters = BuildSnippetSearchFilters(keyword, collection.SnippetIds, isFavourite);
        var finalFilter = Builders<Snippet>.Filter.And(filters);
        return await _snippetsCollection.Find(finalFilter).ToListAsync();
    }

    public async Task<List<Snippet>> SearchSharedInCollectionAsync(string keyword, string? collectionId, bool isFavourite, Users user)
    {
        if (string.IsNullOrEmpty(collectionId) || user == null || user.SharedSnippetIds == null || !user.SharedSnippetIds.Any())
            return new List<Snippet>();

        var collection = await _collectionsCollection.Find(c => c.Id == collectionId).FirstOrDefaultAsync();
        if (collection == null || collection.SnippetIds == null || !collection.SnippetIds.Any())
            return new List<Snippet>();

        var allowedSnippetIds = collection.SnippetIds
            .Where(id => user.SharedSnippetIds.Contains(id))
            .Distinct()
            .ToList();

        if (!allowedSnippetIds.Any())
            return new List<Snippet>();

        var filters = BuildSnippetSearchFilters(keyword, allowedSnippetIds, isFavourite);
        var finalFilter = Builders<Snippet>.Filter.And(filters);
        return await _snippetsCollection.Find(finalFilter).ToListAsync();
    }

    public Task<List<Snippet>> GetForUserAsync(Users user, List<Collection> userCollections)
    {
        if (user == null || user.MyCollectionIds == null || !user.MyCollectionIds.Any())
            return Task.FromResult(new List<Snippet>());

        var snippetIds = userCollections
            .Where(c => user.MyCollectionIds.Contains(c.Id))
            .SelectMany(c => c.SnippetIds)
            .Distinct()
            .ToList();

        if (!snippetIds.Any())
            return Task.FromResult(new List<Snippet>());

        var filter = Builders<Snippet>.Filter.In(s => s.Id, snippetIds);
        return _snippetsCollection.Find(filter).ToListAsync();
    }

    public Task<List<Snippet>> GetSharedForUserAsync(Users user)
    {
        if (user == null || user.SharedSnippetIds == null || !user.SharedSnippetIds.Any())
            return Task.FromResult(new List<Snippet>());

        var filter = Builders<Snippet>.Filter.In(s => s.Id, user.SharedSnippetIds);
        return _snippetsCollection.Find(filter).ToListAsync();
    }

    private static List<FilterDefinition<Snippet>> BuildSnippetSearchFilters(string keyword, List<string> snippetIds, bool isFavourite)
    {
        var filters = new List<FilterDefinition<Snippet>>
        {
            Builders<Snippet>.Filter.In(s => s.Id, snippetIds)
        };

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            filters.Add(Builders<Snippet>.Filter.Or(
                Builders<Snippet>.Filter.Regex(s => s.Title, new BsonRegularExpression(keyword, "i")),
                Builders<Snippet>.Filter.Regex(s => s.Content, new BsonRegularExpression(keyword, "i")),
                Builders<Snippet>.Filter.Regex(s => s.HashtagsInput, new BsonRegularExpression(keyword, "i"))));
        }

        if (isFavourite)
        {
            filters.Add(Builders<Snippet>.Filter.Eq(s => s.IsFavourite, true));
        }

        return filters;
    }
}
