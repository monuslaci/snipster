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
}
