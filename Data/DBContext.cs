using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using MongoDB.Driver;
using MongoDbGenericRepository;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using static Snipster.Data.DBContext;
using Microsoft.Win32;
using AspNetCore.Identity.MongoDbCore.Models;
using MongoDbGenericRepository.Attributes;

namespace Snipster.Data
{
    public class DBContext
    {
        public class Snippet
        {
            [BsonId]
            [BsonRepresentation(BsonType.ObjectId)]
            public string Id { get; set; }

            [Required(ErrorMessage = "Title is required.")]
            public string Title { get; set; }
            public DateTime CreatedDate { get; set; }
            public DateTime LastModifiedDate { get; set; }

            [Required(ErrorMessage = "Content  is required.")]
            public string Content { get; set; }

            [CustomValidation(typeof(Snippet), nameof(ValidateHashtags))]
            public string HashtagsInput { get; set; }

            public string Language { get; set; }

            public static ValidationResult ValidateHashtags(string hashtagsInput, ValidationContext context)
            {
                if (hashtagsInput == null)
                {
                    return ValidationResult.Success; // Tags are optional.
                }

                List<string> hashtags = hashtagsInput
                    .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(tag => tag.Trim())
                    .ToList();

                if (hashtags.Count == 0)
                {
                    return ValidationResult.Success; // Tags are optional.
                }

                foreach (var tag in hashtags)
                {
                    if (!tag.StartsWith("#"))
                    {
                        return new ValidationResult("Each hashtag must start with '#'.");
                    }

                    if (tag.Contains(" "))
                    {
                        return new ValidationResult("Hashtags cannot contain spaces.");
                    }
                }

                return ValidationResult.Success;
            }
        }
        public class Collection
        {
            [BsonId]
            [BsonRepresentation(BsonType.ObjectId)]
            public string Id { get; set; }
            public DateTime LastModifiedDate { get; set; }
            public string Title { get; set; }
            public bool IsPublic { get; set; }
            public string CreatedBy { get; set; }
            public List<string> SnippetIds { get; set; } = new List<string>();
            public string IdString => string.Join(",", Id); // Convert list to string
        }

        public class Lists
        {
            [BsonId]
            [BsonRepresentation(BsonType.ObjectId)]
            public string Id { get; set; }

            public string Title { get; set; }
            public List<string> SnippetIds { get; set; }
        }

        public class Users : MongoIdentityUser<string>
        {
            public Users() : base() { }

            //[BsonId]
            //[BsonRepresentation(BsonType.ObjectId)]
            //public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

            public string FirstName { get; set; }
            public string LastName { get; set; }
            //public string Email { get; set; }     
            //public string PasswordHash { get; set; }
            public bool RegistrationConfirmed { get; set; }
        }
        public class RegisterUserDTO
        {
            [Required(ErrorMessage = "First name is required")]
            [StringLength(50, ErrorMessage = "First name cannot be longer than 50 characters")]
            public string FirstName { get; set; }

            [Required(ErrorMessage = "Last name is required")]
            [StringLength(50, ErrorMessage = "Last name cannot be longer than 50 characters")]
            public string LastName { get; set; }

            [Required(ErrorMessage = "Email is required")]
            [EmailAddress(ErrorMessage = "Invalid email format")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Username is required")]
            [StringLength(20, MinimumLength = 5, ErrorMessage = "Username must be between 5 and 20 characters")]
            public string UserName { get; set; }

            [Required(ErrorMessage = "Password is required")]
            [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
            [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$",  ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter and one number.")]
            public string Password { get; set; }
        }

        [CollectionName("Roles")] // Defines the MongoDB collection name
        public class ApplicationRole : MongoIdentityRole<string>
        {
            public ApplicationRole() : base() { }

            public ApplicationRole(string roleName) : base(roleName) { }
        }

        public class PasswordResetToken
        {
            [BsonId]
            public ObjectId Id { get; set; }

            [BsonElement("email")]
            public string Email { get; set; }

            [BsonElement("token")]
            public string Token { get; set; }

            [BsonElement("expiry")]
            public DateTime Expiry { get; set; }
        }

        public class RegistrationConfirmToken
        {
            [BsonId]
            public ObjectId Id { get; set; }

            [BsonElement("email")]
            public string Email { get; set; }

            [BsonElement("token")]
            public string Token { get; set; }

        }
    }

    public class MongoDbContext : Snipster.Data.IMongoDbContext, MongoDbGenericRepository.IMongoDbContext
    {
        private readonly IMongoDatabase _database;
        private readonly MongoClient _client;

        public MongoDbContext(string connectionString, string databaseName)
        {
            _client = new MongoClient(connectionString); // Initialize MongoDB client
            _database = _client.GetDatabase(databaseName); // Get the MongoDB database
        }

        public IMongoClient Client => _client;

        public IMongoDatabase Database => _database;

        // This allows MongoDbStores to interact with collections
        public IMongoCollection<TDocument> GetCollection<TDocument>(string name)
        {
            return _database.GetCollection<TDocument>(name);
        }

        // Additional MongoDbContext functionality 
        public void DropCollection<TDocument>(string name)
        {
            _database.DropCollection(name);
        }

        public void SetGuidRepresentation(GuidRepresentation representation)
        {
            BsonDefaults.GuidRepresentation = representation;
        }
    }



    public interface IMongoDbContext
    {
        IMongoClient Client { get; }
        IMongoDatabase Database { get; }
        IMongoCollection<TDocument> GetCollection<TDocument>(string name);
        void DropCollection<TDocument>(string name);
    }



    public class MongoUserStore : IUserStore<Users>, IDisposable
    {
        private readonly IMongoCollection<Users> _users;
        private bool _disposed;

        public MongoUserStore(IMongoDatabase database)
        {
            _users = database.GetCollection<Users>("Users");
        }

        public async Task<IdentityResult> CreateAsync(Users user, CancellationToken cancellationToken)
        {
            try
            {
                await _users.InsertOneAsync(user, null, cancellationToken);
                return IdentityResult.Success;
            }
            catch (Exception)
            {
                return IdentityResult.Failed(new IdentityError { Description = "Error occurred while creating the user." });
            }
        }

        public async Task<IdentityResult> DeleteAsync(Users user, CancellationToken cancellationToken)
        {
            try
            {
                var filter = Builders<Users>.Filter.Eq(u => u.Id, user.Id);
                await _users.DeleteOneAsync(filter, cancellationToken);
                return IdentityResult.Success;
            }
            catch (Exception)
            {
                return IdentityResult.Failed(new IdentityError { Description = "Error occurred while deleting the user." });
            }
        }

        public async Task<Users> FindByIdAsync(string userId, CancellationToken cancellationToken)
        {
            var filter = Builders<Users>.Filter.Eq(u => u.Id, userId);
            return await _users.Find(filter).FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<Users> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
        {
            var filter = Builders<Users>.Filter.Eq(u => u.NormalizedUserName, normalizedUserName);
            return await _users.Find(filter).FirstOrDefaultAsync(cancellationToken);
        }

        public Task<string> GetNormalizedUserNameAsync(Users user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.NormalizedUserName);
        }

        public Task<string> GetUserNameAsync(Users user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.UserName);
        }

        public Task SetNormalizedUserNameAsync(Users user, string normalizedName, CancellationToken cancellationToken)
        {
            user.NormalizedUserName = normalizedName;
            return Task.CompletedTask;
        }

        public Task SetUserNameAsync(Users user, string userName, CancellationToken cancellationToken)
        {
            user.UserName = userName;
            return Task.CompletedTask;
        }

        public Task<IdentityResult> UpdateAsync(Users user, CancellationToken cancellationToken)
        {
            try
            {
                var filter = Builders<Users>.Filter.Eq(u => u.Id, user.Id);
                var update = Builders<Users>.Update.Set(u => u.UserName, user.UserName);
                _users.UpdateOne(filter, update, cancellationToken: cancellationToken);
                return Task.FromResult(IdentityResult.Success);
            }
            catch (Exception)
            {
                return Task.FromResult(IdentityResult.Failed(new IdentityError { Description = "Error occurred while updating the user." }));
            }
        }

        public Task<string> GetUserIdAsync(Users user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.Id);
        }

        public void Dispose()
        {
            if (_disposed) return;

            // Perform cleanup if necessary (MongoDB doesn't require special cleanup)
            _disposed = true;
        }
    }



}
