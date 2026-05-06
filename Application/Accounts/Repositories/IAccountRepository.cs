using static Snipster.Data.CommonClasses;
using static Snipster.Data.DBContext;

namespace Snipster.Application.Accounts.Repositories;

public interface IAccountRepository
{
    Task<bool> RegisterUserAsync(Users newUser, string password);
    Task<LoginReturn> ValidateUserAsync(string email, string password);
    Task<Users> GetUserByEmailAsync(string email);
    Task UpdateUserAsync(Users user);
    Task UpdateUserPasswordAsync(Users user, string password);
    Task DeleteUserAccountAsync(string email);
}
