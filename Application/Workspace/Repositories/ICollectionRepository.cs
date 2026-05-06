using static Snipster.Data.DBContext;

namespace Snipster.Application.Workspace.Repositories;

public interface ICollectionRepository
{
    Task<List<Collection>> GetForUserAsync(string email);
    Task<List<Collection>> GetSharedForUserAsync(string email);
    Task AddAsync(Collection collection);
    Task UpdateAsync(Collection collection);
    Task DeleteAsync(string collectionId);
    Task<List<Collection>> GetBySnippetIdAsync(string snippetId);
}
