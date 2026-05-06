using Snipster.Data;
using static Snipster.Data.DBContext;

namespace Snipster.Application.Accounts;

public interface IAccountService
{
    Task<CommonClasses.LoginReturn> ValidateCredentialsAsync(string email, string password);
    Task<AccountActionResult> RegisterAsync(RegisterUserDTO newUser);
    Task<AccountActionResult> ResendRegistrationConfirmationAsync(string email);
    Task<bool> ConfirmRegistrationAsync(string token);
    Task<AccountActionResult> SendPasswordResetAsync(string email);
    Task<bool> ResetPasswordAsync(string token, string newPassword);
    Task UpdateUserAsync(Users user);
    Task UpdateUserPasswordAsync(Users user, string password);
    Task DeleteUserAccountAsync(string email);
}
