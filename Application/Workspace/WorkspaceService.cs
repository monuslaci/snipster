using Snipster.Application.Workspace.Repositories;
using static Snipster.Data.DBContext;

namespace Snipster.Application.Workspace;

public class WorkspaceService : IWorkspaceService
{
    private readonly ICollectionRepository _collectionRepository;
    private readonly ISnippetRepository _snippetRepository;
    private readonly IUserRepository _userRepository;

    public WorkspaceService(
        ICollectionRepository collectionRepository,
        ISnippetRepository snippetRepository,
        IUserRepository userRepository)
    {
        _collectionRepository = collectionRepository;
        _snippetRepository = snippetRepository;
        _userRepository = userRepository;
    }

    public Task<List<Collection>> GetUserCollectionsAsync(string email)
    {
        return _collectionRepository.GetForUserAsync(email);
    }

    public Task<List<Collection>> GetSharedCollectionsAsync(string email)
    {
        return _collectionRepository.GetSharedForUserAsync(email);
    }

    public async Task<Dictionary<string, string>> GetCollectionOwnerNamesAsync(IEnumerable<Collection> collections)
    {
        var ownerNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var ownerEmails = collections
            .Select(c => c.CreatedBy)
            .Where(email => !string.IsNullOrWhiteSpace(email))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var ownerEmail in ownerEmails)
        {
            var owner = await _userRepository.GetByEmailAsync(ownerEmail);
            ownerNames[ownerEmail] = GetUserDisplayName(owner, ownerEmail);
        }

        return ownerNames;
    }

    public async Task CreateCollectionAsync(Collection collection, Users user, string? userEmail)
    {
        collection.CreatedBy = userEmail;
        collection.LastModifiedDate = DateTime.Now;

        await _collectionRepository.AddAsync(collection);

        if (!user.MyCollectionIds.Contains(collection.Id))
        {
            user.MyCollectionIds.Add(collection.Id);
            await _userRepository.UpdateAsync(user);
        }
    }

    public async Task UpdateCollectionAsync(Collection collection, string? userEmail)
    {
        collection.CreatedBy = userEmail;
        collection.LastModifiedDate = DateTime.Now;
        await _collectionRepository.UpdateAsync(collection);
    }

    public Task DeleteCollectionAsync(string collectionId)
    {
        return _collectionRepository.DeleteAsync(collectionId);
    }

    public Task<List<Snippet>> GetCollectionSnippetsAsync(string collectionId, bool isOwnCollection, Users user)
    {
        return isOwnCollection
            ? _snippetRepository.GetByCollectionAsync(collectionId)
            : _snippetRepository.GetSharedByCollectionForUserAsync(collectionId, user);
    }

    public Task<Snippet> GetSnippetAsync(string snippetId)
    {
        return _snippetRepository.GetByIdAsync(snippetId);
    }

    public Task SaveSnippetFavouriteAsync(Snippet snippet)
    {
        return _snippetRepository.SaveAsync(snippet);
    }

    public async Task<string> CreateSnippetAsync(Snippet snippet, string? collectionId, Users user, IEnumerable<Collection> collections)
    {
        snippet.CreatedDate = DateTime.Now;
        snippet.LastModifiedDate = DateTime.Now;
        snippet.CollectionId = collectionId;
        snippet.CreatedBy = user.Id;

        var snippetId = await _snippetRepository.AddAsync(snippet);

        var selectedCollection = collections.FirstOrDefault(c => c.Id == collectionId);
        if (selectedCollection != null)
        {
            if (!selectedCollection.SnippetIds.Contains(snippetId))
            {
                selectedCollection.SnippetIds.Add(snippetId);
            }

            selectedCollection.LastModifiedDate = DateTime.Now;
            await _collectionRepository.UpdateAsync(selectedCollection);
        }

        await AddSharedSnippetReferencesAsync(snippetId, snippet.SharedWith);
        return snippetId;
    }

    public async Task UpdateSnippetAsync(Snippet snippet, string? collectionId, IEnumerable<Collection> collections, IEnumerable<string> previousSharedWith)
    {
        var selectedCollection = collections.FirstOrDefault(c => c.Id == collectionId);
        if (selectedCollection != null && !selectedCollection.SnippetIds.Contains(snippet.Id))
        {
            selectedCollection.SnippetIds.Add(snippet.Id);
            selectedCollection.LastModifiedDate = DateTime.Now;
            await _collectionRepository.UpdateAsync(selectedCollection);
        }

        snippet.LastModifiedDate = DateTime.Now;
        await _snippetRepository.SaveAsync(snippet);

        var previous = previousSharedWith?.ToHashSet(StringComparer.OrdinalIgnoreCase)
            ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var current = snippet.SharedWith?.ToHashSet(StringComparer.OrdinalIgnoreCase)
            ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        await AddSharedSnippetReferencesAsync(snippet.Id, current.Except(previous, StringComparer.OrdinalIgnoreCase));
        await RemoveSharedSnippetReferencesAsync(snippet.Id, previous.Except(current, StringComparer.OrdinalIgnoreCase));
    }

    public async Task DeleteSnippetAsync(string snippetId)
    {
        var snippet = await _snippetRepository.GetByIdAsync(snippetId);
        if (snippet?.SharedWith?.Any() == true)
        {
            await RemoveSharedSnippetReferencesAsync(snippetId, snippet.SharedWith);
        }

        await _snippetRepository.DeleteAsync(snippetId);
    }

    public Task<List<Snippet>> SearchCollectionSnippetsAsync(string keyword, string? collectionId, bool isFavourite, bool isOwnCollection, Users user)
    {
        return isOwnCollection
            ? _snippetRepository.SearchInCollectionAsync(keyword, collectionId, isFavourite)
            : _snippetRepository.SearchSharedInCollectionAsync(keyword, collectionId, isFavourite, user);
    }

    public Task<List<Snippet>> GetUserSnippetsAsync(Users user, List<Collection> userCollections)
    {
        return _snippetRepository.GetForUserAsync(user, userCollections);
    }

    public Task<List<Snippet>> GetSharedSnippetsAsync(Users user)
    {
        return _snippetRepository.GetSharedForUserAsync(user);
    }

    public Task<List<Collection>> GetCollectionsBySnippetIdAsync(string snippetId)
    {
        return _collectionRepository.GetBySnippetIdAsync(snippetId);
    }

    private async Task AddSharedSnippetReferencesAsync(string snippetId, IEnumerable<string> emails)
    {
        foreach (var sharedEmail in emails.Where(email => !string.IsNullOrWhiteSpace(email)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var modifiedUser = await _userRepository.GetByEmailAsync(sharedEmail);
            if (modifiedUser != null && !modifiedUser.SharedSnippetIds.Contains(snippetId))
            {
                modifiedUser.SharedSnippetIds.Add(snippetId);
                await _userRepository.UpdateAsync(modifiedUser);
            }
        }
    }

    private async Task RemoveSharedSnippetReferencesAsync(string snippetId, IEnumerable<string> emails)
    {
        foreach (var sharedEmail in emails.Where(email => !string.IsNullOrWhiteSpace(email)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var modifiedUser = await _userRepository.GetByEmailAsync(sharedEmail);
            if (modifiedUser != null && modifiedUser.SharedSnippetIds.Remove(snippetId))
            {
                await _userRepository.UpdateAsync(modifiedUser);
            }
        }
    }

    private static string GetUserDisplayName(Users owner, string fallback)
    {
        if (owner == null)
            return fallback;

        var displayName = $"{owner.FirstName} {owner.LastName}".Trim();
        return string.IsNullOrWhiteSpace(displayName) ? fallback : displayName;
    }
}
