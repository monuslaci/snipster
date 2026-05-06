using Snipster.Application.Accounts.Repositories;
using Snipster.Services;
using static Snipster.Data.DBContext;
using static Snipster.Data.CommonClasses;

namespace Snipster.Application.Accounts;

public class AccountService : IAccountService
{
    private readonly IAccountRepository _accountRepository;
    private readonly ITokenRepository _tokenRepository;
    private readonly IEmailService _emailService;

    public AccountService(
        IAccountRepository accountRepository,
        ITokenRepository tokenRepository,
        IEmailService emailService)
    {
        _accountRepository = accountRepository;
        _tokenRepository = tokenRepository;
        _emailService = emailService;
    }

    public Task<LoginReturn> ValidateCredentialsAsync(string email, string password)
    {
        return _accountRepository.ValidateUserAsync(email, password);
    }

    public async Task<AccountActionResult> RegisterAsync(RegisterUserDTO newUser)
    {
        if (newUser.Password != newUser.PasswordRepeat)
        {
            return new AccountActionResult
            {
                Success = false,
                Message = "New password must match the repeated password."
            };
        }

        var user = new Users
        {
            FirstName = newUser.FirstName,
            LastName = newUser.LastName,
            Email = newUser.Email,
            UserName = newUser.Email,
            NormalizedEmail = newUser.Email.ToUpperInvariant(),
            NormalizedUserName = newUser.Email.ToUpperInvariant(),
            RegistrationConfirmed = false,
            EmailConfirmed = false
        };

        var registered = await _accountRepository.RegisterUserAsync(user, newUser.Password);
        if (!registered)
        {
            return new AccountActionResult
            {
                Success = false,
                Message = "Registration in not successful, this email address is already registered"
            };
        }

        await SendRegistrationConfirmationAsync(user);

        return new AccountActionResult
        {
            Success = true,
            Message = "Registration successful. Please confirm your account from the email we sent. If you do not see it, check your spam or junk folder."
        };
    }

    public async Task<AccountActionResult> ResendRegistrationConfirmationAsync(string email)
    {
        var user = await _accountRepository.GetUserByEmailAsync(email);
        if (user == null)
        {
            return new AccountActionResult
            {
                Success = false,
                Message = "Email address is not registered"
            };
        }

        if (user.RegistrationConfirmed)
        {
            return new AccountActionResult
            {
                Success = true,
                Message = "Registration is already confirmed. You can log in now."
            };
        }

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            return new AccountActionResult
            {
                Success = false,
                Message = "User email address is missing."
            };
        }

        await SendRegistrationConfirmationAsync(user);

        return new AccountActionResult
        {
            Success = true,
            Message = "Activation email has been sent again. If you do not see it, check your spam or junk folder."
        };
    }

    public Task<bool> ConfirmRegistrationAsync(string token)
    {
        return _tokenRepository.ValidateRegistrationTokenAsync(token);
    }

    public async Task<AccountActionResult> SendPasswordResetAsync(string email)
    {
        var user = await _accountRepository.GetUserByEmailAsync(email);
        if (user == null)
        {
            return new AccountActionResult
            {
                Success = false,
                Message = "User with this email address is not registered"
            };
        }

        var token = await _tokenRepository.GenerateResetTokenAsync(user.Email);
        await _emailService.SendEmailNotification(AccountEmailFactory.CreatePasswordResetEmail(
            user.Email,
            $"{user.FirstName} {user.LastName}",
            token));

        return new AccountActionResult
        {
            Success = true,
            Message = "An email has been sent to your registered email address. If you do not see it, check your spam or junk folder."
        };
    }

    public Task<bool> ResetPasswordAsync(string token, string newPassword)
    {
        return _tokenRepository.ResetPasswordAsync(token, newPassword);
    }

    public Task UpdateUserAsync(Users user)
    {
        return _accountRepository.UpdateUserAsync(user);
    }

    public Task UpdateUserPasswordAsync(Users user, string password)
    {
        return _accountRepository.UpdateUserPasswordAsync(user, password);
    }

    public Task DeleteUserAccountAsync(string email)
    {
        return _accountRepository.DeleteUserAccountAsync(email);
    }

    private async Task SendRegistrationConfirmationAsync(Users user)
    {
        var token = await _tokenRepository.GenerateRegistrationTokenAsync(user.Email);
        await _emailService.SendEmailNotification(AccountEmailFactory.CreateRegistrationEmail(
            user.Email,
            $"{user.FirstName} {user.LastName}",
            token));
    }
}
