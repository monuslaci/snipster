using Snipster.Application.Workspace.Repositories;
using Snipster.Services;
using static Snipster.Data.DBContext;

namespace Snipster.Infrastructure.Repositories;

public class MongoCollectionRepository : ICollectionRepository
{
    private readonly MongoDbService _mongoDbService;

    public MongoCollectionRepository(MongoDbService mongoDbService)
    {
        _mongoDbService = mongoDbService;
    }

    public Task<List<Collection>> GetForUserAsync(string email)
    {
        return _mongoDbService.GetCollectionsForUserAsync(email);
    }

    public Task<List<Collection>> GetSharedForUserAsync(string email)
    {
        return _mongoDbService.GetSharedCollectionsForUserAsync(email);
    }

    public Task AddAsync(Collection collection)
    {
        return _mongoDbService.AddCollectionAsync(collection);
    }

    public Task UpdateAsync(Collection collection)
    {
        return _mongoDbService.UpdateCollectionAsync(collection);
    }

    public Task DeleteAsync(string collectionId)
    {
        return _mongoDbService.DeleteCollectionAsync(collectionId);
    }

    public Task<List<Collection>> GetBySnippetIdAsync(string snippetId)
    {
        return _mongoDbService.GetCollectionsBySnippetId(snippetId);
    }
}
