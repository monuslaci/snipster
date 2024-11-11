using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;

namespace Snipster.Data
{
    public class DBContext
    {
        public class Snippet
        {
            [BsonId]
            [BsonRepresentation(BsonType.ObjectId)]
            public string Id { get; set; }  // MongoDB ID
            public string Title { get; set; }
            public DateTime CreatedDate { get; set; }
            public string Content { get; set; }
            public List<string> Tags { get; set; }  
            public string Language { get; set; }
        }
        public class Collection
        {
            [BsonId]
            [BsonRepresentation(BsonType.ObjectId)]
            public string Id { get; set; }  

            public string Title { get; set; }
            public bool IsPublic { get; set; }
            public List<string> SnippetIds { get; set; }  
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
