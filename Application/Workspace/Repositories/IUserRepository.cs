using static Snipster.Data.DBContext;

namespace Snipster.Application.Workspace.Repositories;

public interface IUserRepository
{
    Task<Users> GetByEmailAsync(string email);
    Task UpdateAsync(Users user);
}
