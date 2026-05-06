using MongoDB.Driver;
using System.Security.Cryptography;
using Snipster.Application.Accounts.Repositories;
using static Snipster.Data.DBContext;

namespace Snipster.Infrastructure.Repositories;

public class MongoTokenRepository : ITokenRepository
{
    private readonly IMongoCollection<PasswordResetToken> _tokensCollection;
    private readonly IMongoCollection<RegistrationConfirmToken> _registrationTokensCollection;
    private readonly IMongoCollection<Users> _usersCollection;

    public MongoTokenRepository(IMongoDatabase database)
    {
        _tokensCollection = database.GetCollection<PasswordResetToken>("PasswordResetTokens");
        _registrationTokensCollection = database.GetCollection<RegistrationConfirmToken>("RegistrationConfirmTokens");
        _usersCollection = database.GetCollection<Users>("Users");
    }

    public async Task<string> GenerateResetTokenAsync(string email)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var resetToken = new PasswordResetToken
        {
            Email = email,
            Token = token,
            Expiry = DateTime.UtcNow.AddHours(1)
        };

        await _tokensCollection.InsertOneAsync(resetToken);
        return token;
    }

    public async Task<bool> ResetPasswordAsync(string token, string newPassword)
    {
        var tokenRecord = await _tokensCollection.Find(t => t.Token == token).FirstOrDefaultAsync();
        if (tokenRecord == null || tokenRecord.Expiry < DateTime.UtcNow)
            return false;

        var update = Builders<Users>.Update.Set(u => u.PasswordHash, BCrypt.Net.BCrypt.HashPassword(newPassword));
        await _usersCollection.UpdateOneAsync(u => u.Email == tokenRecord.Email, update);
        await _tokensCollection.DeleteOneAsync(t => t.Token == token);

        return true;
    }

    public async Task<string> GenerateRegistrationTokenAsync(string email)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var registrationToken = new RegistrationConfirmToken
        {
            Email = email,
            Token = token
        };

        await _registrationTokensCollection.InsertOneAsync(registrationToken);
        return token;
    }

    public async Task<bool> ValidateRegistrationTokenAsync(string token)
    {
        var tokenRecord = await _registrationTokensCollection.Find(t => t.Token == token).FirstOrDefaultAsync();
        if (tokenRecord == null)
            return false;

        var update = Builders<Users>.Update.Set(u => u.RegistrationConfirmed, true);
        await _usersCollection.UpdateOneAsync(u => u.Email == tokenRecord.Email, update);
        await _registrationTokensCollection.DeleteOneAsync(t => t.Token == token);

        return true;
    }
}
