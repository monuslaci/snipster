using static Snipster.Data.DBContext;

namespace Snipster.Application.Workspace;

public interface IWorkspaceService
{
    Task<List<Collection>> GetUserCollectionsAsync(string email);
    Task<List<Collection>> GetSharedCollectionsAsync(string email);
    Task<Dictionary<string, string>> GetCollectionOwnerNamesAsync(IEnumerable<Collection> collections);
    Task CreateCollectionAsync(Collection collection, Users user, string? userEmail);
    Task UpdateCollectionAsync(Collection collection, string? userEmail);
    Task DeleteCollectionAsync(string collectionId);
    Task<List<Snippet>> GetCollectionSnippetsAsync(string collectionId, bool isOwnCollection, Users user);
    Task<Snippet> GetSnippetAsync(string snippetId);
    Task SaveSnippetFavouriteAsync(Snippet snippet);
    Task<string> CreateSnippetAsync(Snippet snippet, string? collectionId, Users user, IEnumerable<Collection> collections);
    Task UpdateSnippetAsync(Snippet snippet, string? collectionId, IEnumerable<Collection> collections, IEnumerable<string> previousSharedWith);
    Task DeleteSnippetAsync(string snippetId);
    Task<List<Snippet>> SearchCollectionSnippetsAsync(string keyword, string? collectionId, bool isFavourite, bool isOwnCollection, Users user);
    Task<List<Snippet>> GetUserSnippetsAsync(Users user, List<Collection> userCollections);
    Task<List<Snippet>> GetSharedSnippetsAsync(Users user);
    Task<List<Collection>> GetCollectionsBySnippetIdAsync(string snippetId);
}
