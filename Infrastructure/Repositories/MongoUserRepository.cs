using MongoDB.Driver;
using Snipster.Application.Workspace.Repositories;
using static Snipster.Data.DBContext;

namespace Snipster.Infrastructure.Repositories;

public class MongoUserRepository : IUserRepository
{
    private readonly IMongoCollection<Users> _usersCollection;

    public MongoUserRepository(IMongoDatabase database)
    {
        _usersCollection = database.GetCollection<Users>("Users");
    }

    public Task<Users> GetByEmailAsync(string email)
    {
        return _usersCollection.Find(u => u.Email == email).FirstOrDefaultAsync();
    }

    public Task<List<Users>> GetAllAsync()
    {
        return _usersCollection.Find(_ => true).ToListAsync();
    }

    public async Task UpdateAsync(Users user)
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
}
