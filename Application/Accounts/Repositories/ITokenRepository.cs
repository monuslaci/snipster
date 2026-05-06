namespace Snipster.Application.Accounts.Repositories;

public interface ITokenRepository
{
    Task<string> GenerateResetTokenAsync(string email);
    Task<bool> ResetPasswordAsync(string token, string newPassword);
    Task<string> GenerateRegistrationTokenAsync(string email);
    Task<bool> ValidateRegistrationTokenAsync(string token);
}
