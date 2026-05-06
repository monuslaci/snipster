using Snipster.Application.Workspace.Repositories;
using Snipster.Services;
using static Snipster.Data.DBContext;

namespace Snipster.Infrastructure.Repositories;

public class MongoSnippetRepository : ISnippetRepository
{
    private readonly MongoDbService _mongoDbService;

    public MongoSnippetRepository(MongoDbService mongoDbService)
    {
        _mongoDbService = mongoDbService;
    }

    public Task<string> AddAsync(Snippet snippet)
    {
        return _mongoDbService.AddSnippetAsync(snippet);
    }

    public Task<Snippet> GetByIdAsync(string snippetId)
    {
        return _mongoDbService.GetSnippetByIdAsync(snippetId);
    }

    public Task SaveAsync(Snippet snippet)
    {
        return _mongoDbService.SaveSnippetAsync(snippet);
    }

    public Task DeleteAsync(string snippetId)
    {
        return _mongoDbService.DeleteSnippetAsync(snippetId);
    }

    public Task<List<Snippet>> GetByCollectionAsync(string collectionId)
    {
        return _mongoDbService.GetSnippetsByCollectionAsync(collectionId);
    }

    public Task<List<Snippet>> GetSharedByCollectionForUserAsync(string collectionId, Users user)
    {
        return _mongoDbService.GetSharedSnippetsByCollectionForUserAsync(collectionId, user);
    }

    public Task<List<Snippet>> SearchInCollectionAsync(string keyword, string? collectionId, bool isFavourite)
    {
        return _mongoDbService.SearchSnippetInSelectedCollectionAsync(keyword, collectionId, isFavourite);
    }

    public Task<List<Snippet>> SearchSharedInCollectionAsync(string keyword, string? collectionId, bool isFavourite, Users user)
    {
        return _mongoDbService.SearchSharedSnippetInSelectedCollectionAsync(keyword, collectionId, isFavourite, user);
    }

    public Task<List<Snippet>> GetForUserAsync(Users user, List<Collection> userCollections)
    {
        return _mongoDbService.GetSnippetsByUserAsync(user, userCollections);
    }

    public Task<List<Snippet>> GetSharedForUserAsync(Users user)
    {
        return _mongoDbService.GetSharedSnippetsByUserAsync(user);
    }
}
