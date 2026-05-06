using Snipster.Application.Workspace.Repositories;
using Snipster.Services;
using static Snipster.Data.DBContext;

namespace Snipster.Infrastructure.Repositories;

public class MongoUserRepository : IUserRepository
{
    private readonly MongoDbService _mongoDbService;

    public MongoUserRepository(MongoDbService mongoDbService)
    {
        _mongoDbService = mongoDbService;
    }

    public Task<Users> GetByEmailAsync(string email)
    {
        return _mongoDbService.GetUser(email);
    }

    public Task UpdateAsync(Users user)
    {
        return _mongoDbService.UpdateUser(user);
    }
}
