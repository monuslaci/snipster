using MongoDB.Driver;
using Snipster.Application.Accounts.Repositories;
using static Snipster.Data.CommonClasses;
using static Snipster.Data.DBContext;

namespace Snipster.Infrastructure.Repositories;

public class MongoAccountRepository : IAccountRepository
{
    private readonly IMongoCollection<Collection> _collectionsCollection;
    private readonly IMongoCollection<Snippet> _snippetsCollection;
    private readonly IMongoCollection<Users> _usersCollection;
    private readonly IMongoCollection<PasswordResetToken> _tokensCollection;
    private readonly IMongoCollection<RegistrationConfirmToken> _registrationTokensCollection;

    public MongoAccountRepository(IMongoDatabase database)
    {
        _collectionsCollection = database.GetCollection<Collection>("Collections");
        _snippetsCollection = database.GetCollection<Snippet>("Snippets");
        _usersCollection = database.GetCollection<Users>("Users");
        _tokensCollection = database.GetCollection<PasswordResetToken>("PasswordResetTokens");
        _registrationTokensCollection = database.GetCollection<RegistrationConfirmToken>("RegistrationConfirmTokens");
    }

    public async Task<bool> RegisterUserAsync(Users newUser, string password)
    {
        newUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);

        var existingUser = await _usersCollection.Find(u => u.Email == newUser.Email).FirstOrDefaultAsync();
        if (existingUser != null)
            return false;

        await _usersCollection.InsertOneAsync(newUser);
        return true;
    }

    public async Task<LoginReturn> ValidateUserAsync(string email, string password)
    {
        var user = await _usersCollection.Find(u => u.Email == email).FirstOrDefaultAsync();
        var returnValue = new LoginReturn();

        if (user == null)
        {
            returnValue.Result = false;
            returnValue.Description = "Email address is not registered";
            return returnValue;
        }

        var passwordMatches = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        if (!passwordMatches)
        {
            returnValue.Result = false;
            returnValue.Description = "Email and password does not match";
        }
        else if (!user.RegistrationConfirmed)
        {
            returnValue.Result = false;
            returnValue.Description = "Please confirm your registration before logging in.";
        }
        else
        {
            returnValue.Result = true;
        }

        return returnValue;
    }

    public Task<Users> GetUserByEmailAsync(string email)
    {
        return _usersCollection.Find(u => u.Email == email).FirstOrDefaultAsync();
    }

    public async Task UpdateUserAsync(Users user)
    {
        var filter = Builders<Users>.Filter.Eq(c => c.Email, user.Email);
        var update = Builders<Users>.Update
            .Set(c => c.RegistrationConfirmed, user.RegistrationConfirmed)
            .Set(c => c.FirstName, user.FirstName)
            .Set(c => c.LastName, user.LastName)
            .Set(c => c.MyCollectionIds, user.MyCollectionIds)
            .Set(c => c.SharedSnippetIds, user.SharedSnippetIds);

        await _usersCollection.UpdateOneAsync(filter, update);
    }

    public async Task UpdateUserPasswordAsync(Users user, string password)
    {
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);

        var filter = Builders<Users>.Filter.Eq(c => c.Email, user.Email);
        var update = Builders<Users>.Update
            .Set(c => c.RegistrationConfirmed, user.RegistrationConfirmed)
            .Set(c => c.FirstName, user.FirstName)
            .Set(c => c.LastName, user.LastName)
            .Set(c => c.PasswordHash, user.PasswordHash);

        await _usersCollection.UpdateOneAsync(filter, update);
    }

    public async Task DeleteUserAccountAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return;

        var user = await _usersCollection.Find(u => u.Email == email).FirstOrDefaultAsync();
        if (user == null)
            return;

        var ownedCollectionIds = new HashSet<string>(user.MyCollectionIds ?? new List<string>());

        var ownedCollectionsByCreator = await _collectionsCollection
            .Find(c => c.CreatedBy == email)
            .ToListAsync();

        foreach (var collection in ownedCollectionsByCreator)
        {
            ownedCollectionIds.Add(collection.Id);
        }

        var ownedCollections = ownedCollectionIds.Any()
            ? await _collectionsCollection
                .Find(Builders<Collection>.Filter.In(c => c.Id, ownedCollectionIds))
                .ToListAsync()
            : new List<Collection>();

        var snippetIdsToDelete = ownedCollections
            .SelectMany(c => c.SnippetIds ?? new List<string>())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet();

        var snippetsCreatedByUser = await _snippetsCollection
            .Find(s => s.CreatedBy == user.Id || s.CreatedBy == email)
            .ToListAsync();

        foreach (var snippet in snippetsCreatedByUser)
        {
            snippetIdsToDelete.Add(snippet.Id);
        }

        if (ownedCollectionIds.Any())
        {
            await _collectionsCollection.DeleteManyAsync(
                Builders<Collection>.Filter.In(c => c.Id, ownedCollectionIds));
        }

        if (snippetIdsToDelete.Any())
        {
            var snippetIds = snippetIdsToDelete.ToList();

            await _collectionsCollection.UpdateManyAsync(
                Builders<Collection>.Filter.AnyIn(c => c.SnippetIds, snippetIds),
                Builders<Collection>.Update.PullAll(c => c.SnippetIds, snippetIds));

            await _usersCollection.UpdateManyAsync(
                Builders<Users>.Filter.AnyIn(u => u.SharedSnippetIds, snippetIds),
                Builders<Users>.Update.PullAll(u => u.SharedSnippetIds, snippetIds));

            await _snippetsCollection.DeleteManyAsync(
                Builders<Snippet>.Filter.In(s => s.Id, snippetIds));
        }

        await _snippetsCollection.UpdateManyAsync(
            Builders<Snippet>.Filter.AnyEq(s => s.SharedWith, email),
            Builders<Snippet>.Update.Pull(s => s.SharedWith, email));

        await _tokensCollection.DeleteManyAsync(t => t.Email == email);
        await _registrationTokensCollection.DeleteManyAsync(t => t.Email == email);
        await _usersCollection.DeleteOneAsync(u => u.Email == email);
    }
}
