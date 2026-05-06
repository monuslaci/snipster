using static Snipster.Data.DBContext;

namespace Snipster.Application.Workspace.Repositories;

public interface ISnippetRepository
{
    Task<string> AddAsync(Snippet snippet);
    Task<Snippet> GetByIdAsync(string snippetId);
    Task SaveAsync(Snippet snippet);
    Task DeleteAsync(string snippetId);
    Task<List<Snippet>> GetByCollectionAsync(string collectionId);
    Task<List<Snippet>> GetSharedByCollectionForUserAsync(string collectionId, Users user);
    Task<List<Snippet>> SearchInCollectionAsync(string keyword, string? collectionId, bool isFavourite);
    Task<List<Snippet>> SearchSharedInCollectionAsync(string keyword, string? collectionId, bool isFavourite, Users user);
    Task<List<Snippet>> GetForUserAsync(Users user, List<Collection> userCollections);
    Task<List<Snippet>> GetSharedForUserAsync(Users user);
}
