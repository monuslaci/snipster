using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

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
            public List<string> Tags { get; set; }
            public string Language { get; set; }

            public static ValidationResult ValidateHashtags(string hashtags, ValidationContext context)
            {
                if (string.IsNullOrWhiteSpace(hashtags))
                {
                    return ValidationResult.Success; // Hashtags are optional.
                }

                var tags = hashtags.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                foreach (var tag in tags)
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

        public class Users
        {
            [BsonId]
            [BsonRepresentation(BsonType.ObjectId)]
            public string Id { get; set; }

            public string FirstName { get; set; }
            public string LastName { get; set; }
        }

    }
}
