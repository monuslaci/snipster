using Microsoft.AspNetCore.Identity;
using MongoDB.Driver;
using static Snipster.Data.DBContext;
using BCrypt.Net;
using static Snipster.Data.CommonClasses;
using System.Security.Cryptography;
using MongoDB.Bson;

namespace Snipster.Services
{

    public class MongoDbService 
    {
        private readonly IMongoCollection<Snippet> _snippetsCollection;
        private readonly IMongoCollection<Collection> _collectionsCollection;
        private readonly IMongoCollection<Users> _usersCollection;
        private readonly IMongoCollection<PasswordResetToken> _tokensCollection;
        private readonly IMongoCollection<RegistrationConfirmToken> _registrationTokensCollection;

        private readonly string _connectionString;
        private readonly string _databaseName;
        private readonly IPasswordHasher<Users> _passwordHasher;

        public MongoDbService(string connectionString, string databaseName, IPasswordHasher<Users> passwordHasher)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _databaseName = databaseName ?? throw new ArgumentNullException(nameof(databaseName));
            _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));

            var client = new MongoClient(_connectionString);
            var database = client.GetDatabase(_databaseName);

            //initialize the collections
            _snippetsCollection = database.GetCollection<Snippet>("Snippets");
            _collectionsCollection = database.GetCollection<Collection>("Collections");
            _usersCollection = database.GetCollection<Users>("Users");
            _tokensCollection = database.GetCollection<PasswordResetToken>("PasswordResetTokens");
            _registrationTokensCollection = database.GetCollection<RegistrationConfirmToken>("RegistrationConfirmTokens");
        }

        #region Snippets
        public async Task<string> AddSnippetAsync(Snippet snippet)
        {
            await _snippetsCollection.InsertOneAsync(snippet);
            return snippet.Id;
        }

        public async Task<List<Snippet>> GetAllSnippetsAsync()
        {
            return await _snippetsCollection.Find(_ => true).ToListAsync();
        }

        public async Task<List<Snippet>> GetSnippetsByUserAsync(string email)
        {
            // Find collections where CreatedBy matches the actual user
            var userCollections = await _collectionsCollection
                .Find(collection => collection.CreatedBy == email)
                .ToListAsync();

            // Extract all snippet IDs from the user's collections
            var snippetIds = userCollections.SelectMany(col => col.SnippetIds).Distinct().ToList();

            if (!snippetIds.Any())
            {
                return new List<Snippet>(); // No snippets found
            }

            // Find snippets with the extracted IDs
            return await _snippetsCollection
                .Find(snippet => snippetIds.Contains(snippet.Id))
                .ToListAsync();
        }

        public async Task<Snippet> GetSnippetByIdAsync(string id)
        {
            return await _snippetsCollection.Find(s => s.Id == id).FirstOrDefaultAsync();
        }

        public async Task<List<string>> GetSnippetIdsByCollectionAsync(string collectionId)
        {
            var collection = await _collectionsCollection.Find(c => c.Id == collectionId).FirstOrDefaultAsync();
            if (collection != null)
            {
                return collection.SnippetIds;
            }
            return new List<string>();
        }

        public async Task<List<Snippet>> GetSnippetsByCollectionAsync(string collectionId)
        {
            var collection = await _collectionsCollection.Find(c => c.Id == collectionId).FirstOrDefaultAsync();

            if (collection != null && collection.SnippetIds != null && collection.SnippetIds.Any())
            {
                var snippetIds = collection.SnippetIds;
                var snippets = await _snippetsCollection.Find(snippet => snippetIds.Contains(snippet.Id)).ToListAsync();
                return snippets;
            }
            return new List<Snippet>();
        }


        public async Task SaveSnippetAsync(Snippet snippet)
        {
            // Insert or update snippet
            var existingSnippet = await _snippetsCollection.Find(s => s.Id == snippet.Id).FirstOrDefaultAsync();
            if (existingSnippet == null)
            {
                await _snippetsCollection.InsertOneAsync(snippet);  // Insert if new
            }
            else
            {
                var filter = Builders<Snippet>.Filter.Eq(s => s.Id, snippet.Id);
                var update = Builders<Snippet>.Update
                    .Set(s => s.Title, snippet.Title)
                    .Set(s => s.Content, snippet.Content)
                    .Set(s => s.HashtagsInput, snippet.HashtagsInput)
                    .Set(s => s.LastModifiedDate, snippet.LastModifiedDate)
                    .Set(s => s.CreatedDate, snippet.CreatedDate);
                await _snippetsCollection.UpdateOneAsync(filter, update);  // Update if exists
            }
        }

        public async Task UpdateSnippetAsync(Snippet snippet)
        {
            var filter = Builders<Snippet>.Filter.Eq(s => s.Id, snippet.Id);
            var update = Builders<Snippet>.Update
                .Set(s => s.Title, snippet.Title)
                .Set(s => s.Content, snippet.Content)
                .Set(s => s.HashtagsInput, snippet.HashtagsInput)
                .Set(s => s.LastModifiedDate, snippet.LastModifiedDate)
                .Set(s => s.CreatedDate, snippet.CreatedDate);

            await _snippetsCollection.UpdateOneAsync(filter, update);
        }

        public async Task DeleteSnippetAsync(string id)
        {
            await _snippetsCollection.DeleteOneAsync(s => s.Id == id);
        }


        public async Task<List<Snippet>> SearchSnippetAsync(string keyword, string email)
        {

            var userCollections = await _collectionsCollection
                .Find(collection => collection.CreatedBy == email)
                .ToListAsync();

            // Extract all snippet IDs from the user's collections
            var snippetIds = userCollections.SelectMany(col => col.SnippetIds)
                .Distinct()
                .ToList();

            if (!snippetIds.Any())
            {
                return new List<Snippet>(); // No snippets found
            }

            // Find snippets belonging to the user's collections
            var snippetsForUser = await _snippetsCollection
                .Find(snippet => snippetIds.Contains(snippet.Id))
                .ToListAsync();

            var keywordFilter = Builders<Snippet>.Filter.Or(
                Builders<Snippet>.Filter.Regex(s => s.Id, new BsonRegularExpression(keyword, "i")),
                Builders<Snippet>.Filter.Regex(s => s.Title, new BsonRegularExpression(keyword, "i")),
                Builders<Snippet>.Filter.Regex(s => s.Content, new BsonRegularExpression(keyword, "i")),
                Builders<Snippet>.Filter.Regex(s => s.HashtagsInput, new BsonRegularExpression(keyword, "i"))
            );


            var finalFilteredSnippets = await _snippetsCollection
                .Find(Builders<Snippet>.Filter.And(
                    Builders<Snippet>.Filter.In(s => s.Id, snippetIds), // Ensure snippets belong to the user
                    keywordFilter // Apply keyword filter
                ))
                .ToListAsync();

            return finalFilteredSnippets;
        }


        public async Task<List<Snippet>> SearchSnippetInSelectedCollectionAsync(string keyword, string collectionId)
        {

            if (string.IsNullOrEmpty(collectionId))
            {
                return new List<Snippet>(); // No collection ID provided
            }

            // Retrieve the collection by ID
            var collection = await _collectionsCollection
                .Find(c => c.Id == collectionId)
                .FirstOrDefaultAsync();

            if (collection == null || collection.SnippetIds == null || !collection.SnippetIds.Any())
            {
                return new List<Snippet>(); // No collection found or no snippets in collection
            }

            // Retrieve snippets that belong to the collection
            var snippetFilter = Builders<Snippet>.Filter.In(s => s.Id, collection.SnippetIds);

            var keywordFilter = Builders<Snippet>.Filter.Or(
                Builders<Snippet>.Filter.Regex(s => s.Title, new BsonRegularExpression(keyword, "i")),
                Builders<Snippet>.Filter.Regex(s => s.Content, new BsonRegularExpression(keyword, "i")),
                Builders<Snippet>.Filter.Regex(s => s.HashtagsInput, new BsonRegularExpression(keyword, "i"))
            );


            // Combine filters (ensure snippet belongs to the collection and matches the keyword)
            var finalFilter = Builders<Snippet>.Filter.And(snippetFilter, keywordFilter);


            return await _snippetsCollection.Find(finalFilter).ToListAsync();
        }
        #endregion


        #region Collections
        public async Task<List<Collection>> GetCollectionsBySnippetId(string snippetId)
        {
            var filter = Builders<Collection>.Filter.AnyIn(c => c.SnippetIds, new[] { snippetId });
            return await _collectionsCollection.Find(filter).ToListAsync();
        }

        public async Task UpdateCollectionsAsync(List<Collection> collections)
        {
            foreach (var collection in collections)
            {
                // For each collection, we need to update the list of SnippetIds with the new snippet IDs
                var filter = Builders<Collection>.Filter.Eq(c => c.Id, collection.Id);
                var update = Builders<Collection>.Update.Set(c => c.SnippetIds, collection.SnippetIds);
                await _collectionsCollection.UpdateOneAsync(filter, update);
            }
        }

        public async Task UpdateCollectionAsync(Collection collection)
        {
            var filter = Builders<Collection>.Filter.Eq(c => c.Id, collection.Id);
            var update = Builders<Collection>.Update.Set(c => c.SnippetIds, collection.SnippetIds)
                                                            .Set(c => c.Title, collection.Title)
                                                            .Set(c => c.IsPublic, collection.IsPublic)
                                                            .Set(c => c.CreatedBy, collection.CreatedBy)
                                                            .Set(c => c.LastModifiedDate, collection.LastModifiedDate);
            await _collectionsCollection.UpdateOneAsync(filter, update);
        }
        public async Task CreateCollectionAsync(Collection collection)
        {
            await _collectionsCollection.InsertOneAsync(collection);
        }

        public async Task<List<Collection>> GetCollectionsAsync()
        {
            return await _collectionsCollection.Find(_ => true).ToListAsync();
        }

        public async Task<List<Collection>> GetCollectionsForUserAsync(string email)
        {
            return await _collectionsCollection.Find(c => c.CreatedBy == email).ToListAsync();
        }

        public async Task<List<Collection>> GetLast5CollectionsForUserAsync(string email)
        {
            var collections = await _collectionsCollection
                .Find(Builders<Collection>.Filter.Eq(c => c.CreatedBy, email))
                .Sort(Builders<Collection>.Sort.Descending(c => c.LastModifiedDate))
                .Limit(5)
                .ToListAsync();

            return collections;
        }

        public async Task AddCollectionAsync(Collection collection)
        {
            await _collectionsCollection.InsertOneAsync(collection);
        }

        public async Task DeleteCollectionAsync(string id)
        {
            await _collectionsCollection.DeleteOneAsync(c => c.Id == id);
        }

        public async Task<List<Collection>> SearchCollectionAsync(string keyword, string email)
        {
            var keywordFilter = Builders<Collection>.Filter.Or(
                Builders<Collection>.Filter.Regex(c => c.Id, new BsonRegularExpression(keyword, "i")),
                Builders<Collection>.Filter.Regex(c => c.Title, new BsonRegularExpression(keyword, "i"))
            );

            var emailFilter = Builders<Collection>.Filter.Eq(c => c.CreatedBy, email);

            var filter = Builders<Collection>.Filter.And(keywordFilter, emailFilter);

            return await _collectionsCollection.Find(filter).ToListAsync();
        }

        //public async Task CreateListAsync(Lists list)
        //{
        //    await _lists.InsertOneAsync(list);
        //}

        #endregion  


        #region Users
        public async Task<bool> RegisterUserAsync(Users newUser, string password)
        {
            // Hash the password before storing (similar to Identity)
            newUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);

            // Check if the user already exists
            var existingUser = await _usersCollection.Find(u => u.Email == newUser.Email).FirstOrDefaultAsync();
            if (existingUser != null)
            {
                return false; // User already exists
            }

            // Insert the new user into the database
            await _usersCollection.InsertOneAsync(newUser);
            return true; // Registration successful
        }


        public async Task<LoginReturn> ValidateUserAsync(string email, string password)
        {
            var user = await _usersCollection.Find(u => u.Email == email).FirstOrDefaultAsync();
            LoginReturn returnValue = new LoginReturn();

            if (user == null)
            {
                returnValue.Result = false;
                returnValue.Description = "Email address is not registered";
                return returnValue; 
            }

            bool pwHashResult = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);

            if (!pwHashResult)
            {
                returnValue.Result = false;
                returnValue.Description = "Email and password does not match";
            }
            else
            {
                returnValue.Result = true;
            }

            return returnValue;


        }

        public async Task<Users> GetUser(string email)
        {
            var user = await _usersCollection.Find(u => u.Email == email).FirstOrDefaultAsync();

            return user;
        }

        public async Task UpdateUser(Users user)
        {
            var filter = Builders<Users>.Filter.Eq(c => c.Email, user.Id);
            var update = Builders<Users>.Update.Set(c => c.RegistrationConfirmed, user.RegistrationConfirmed)
                                                            .Set(c => c.FirstName, user.FirstName)
                                                            .Set(c => c.LastName, user.LastName);

            await _usersCollection.UpdateOneAsync(filter, update);
        }
        #endregion


        #region Token
        public async Task<string> GenerateResetTokenAsync(string email)
        {
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)); // Secure random token
            var expiry = DateTime.UtcNow.AddHours(1); // 1-hour expiration

            var resetToken = new PasswordResetToken
            {
                Email = email,
                Token = token,
                Expiry = expiry
            };

            await _tokensCollection.InsertOneAsync(resetToken);
            return token;
        }

        // Validate the token and reset the password
        public async Task<bool> ResetPasswordAsync(string token, string newPassword)
        {
            var tokenRecord = await _tokensCollection.Find(t => t.Token == token).FirstOrDefaultAsync();

            if (tokenRecord == null || tokenRecord.Expiry < DateTime.UtcNow)
                return false; // Invalid or expired token

            var update = Builders<Users>.Update.Set(u => u.PasswordHash, BCrypt.Net.BCrypt.HashPassword(newPassword));
            await _usersCollection.UpdateOneAsync(u => u.Email == tokenRecord.Email, update);

            // Remove the used token
            await _tokensCollection.DeleteOneAsync(t => t.Token == token);
            return true;
        }

        public async Task<string> GenerateRegisterTokenAsync(string email)
        {
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)); // Secure random token

            var resetToken = new RegistrationConfirmToken
            {
                Email = email,
                Token = token,
            };

            await _registrationTokensCollection.InsertOneAsync(resetToken);
            return token;
        }

        // Validate the registration token
        public async Task<bool> ValidateGeneratedRegisterTokenAsync(string token)
        {
            var tokenRecord = await _registrationTokensCollection.Find(t => t.Token == token).FirstOrDefaultAsync();

            if (tokenRecord == null)
                return false; // Invalid token

            // Remove the used token
            await _registrationTokensCollection.DeleteOneAsync(t => t.Token == token);
            return true;
        }
    }
    #endregion
}

