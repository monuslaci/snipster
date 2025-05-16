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
using SendGrid.Helpers.Mail;
using SendGrid;
using Snipster.Data;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Components;
using Snipster.Services.AppStates;
using Snipster.Services;

namespace Snipster.Helpers
{
    public interface IGeneralHelpers
    {
        List<Snippet> SearchSnippetFromMemory(string keyword, Users user, List<Collection> collections, bool isFavouriteSearch);
        List<Snippet> SearchSharedSnippetFromMemory(string keyword, Users user, bool isFavouriteSearch);
        Task<List<Collection>> SearchCollectionAsync(string keyword, string email, List<Collection> userCollections);
        Task UpdateCollectionInMemory(Collection editCollection);
        Task DeleteCollectionFromMemory(string collectionId);
        Task UpdateSnippetInMemory(string selectedCollectionId, Snippet selectedSnippet);
        Task AddNewSnippetInMemory(string selectedCollectionIdCreate, Snippet newSnippet, string snippetId);
        Task DeleteSnippetFromMemory(string snippetId);
    }

    public class GeneralHelpers : IGeneralHelpers
    {
        private readonly MongoDbService _mongoDbService;
        private readonly AppState _appState;

        public GeneralHelpers(MongoDbService mongoDbService, AppState appState)
        {
            _mongoDbService = mongoDbService;
            _appState = appState;
        }

        public List<Snippet> SearchSnippetFromMemory(string keyword, Users user, List<Collection> collections, bool isFavouriteSearch)
        {
            if (user == null || collections == null || collections.Count == 0)
                return new List<Snippet>();

            // Get all snippet IDs from the user's collections
            var validCollectionIds = user.MyCollectionIds.Intersect(collections.Select(c => c.Id)).ToHashSet();

            // Step 1: Flatten relevant snippets from loadedSnippets
            var userSnippets = _appState.loadedSnippets
                .Where(ls => validCollectionIds.Contains(ls.collectionId)) // Only user’s collections
                .SelectMany(ls => ls.snippetList)
                .DistinctBy(s => s.Id) // prevent duplicates if a snippet appears in multiple collections
                .ToList();

            // Step 2: Apply filters
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var keywordLower = keyword.ToLowerInvariant();
                userSnippets = userSnippets.Where(s =>
                    (s.Id?.ToLowerInvariant().Contains(keywordLower) ?? false) ||
                    (s.Title?.ToLowerInvariant().Contains(keywordLower) ?? false) ||
                    (s.Content?.ToLowerInvariant().Contains(keywordLower) ?? false) ||
                    (s.HashtagsInput?.ToLowerInvariant().Contains(keywordLower) ?? false)
                ).ToList();
            }

            if (isFavouriteSearch)
            {
                userSnippets = userSnippets.Where(s => s.IsFavourite).ToList();
            }

            return userSnippets;
        }

        public List<Snippet> SearchSharedSnippetFromMemory(string keyword, Users user, bool isFavouriteSearch)
        {
            if (user == null || user.SharedSnippetIds == null || user.SharedSnippetIds.Count == 0)
                return new List<Snippet>();

            var sharedSnippetIdSet = user.SharedSnippetIds.ToHashSet();

            // Step 1: Extract relevant shared snippets from memory
            var sharedSnippets = _appState.loadedSharedSnippets
                .SelectMany(s => s.snippetList)
                .Where(s => sharedSnippetIdSet.Contains(s.Id))
                .DistinctBy(s => s.Id)
                .ToList();

            // Step 2: Filter by keyword (if any)
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var keywordLower = keyword.ToLowerInvariant();
                sharedSnippets = sharedSnippets.Where(s =>
                    (s.Id?.ToLowerInvariant().Contains(keywordLower) ?? false) ||
                    (s.Title?.ToLowerInvariant().Contains(keywordLower) ?? false) ||
                    (s.Content?.ToLowerInvariant().Contains(keywordLower) ?? false) ||
                    (s.HashtagsInput?.ToLowerInvariant().Contains(keywordLower) ?? false)
                ).ToList();
            }

            // Step 3: Filter by favourite flag (if needed)
            if (isFavouriteSearch)
            {
                sharedSnippets = sharedSnippets.Where(s => s.IsFavourite).ToList();
            }

            return sharedSnippets;
        }

        public async Task<List<Collection>> SearchCollectionAsync(string keyword, string email, List<Collection> userCollections)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                // No keyword: return all user-created collections
                return userCollections
                    .Where(c => c.CreatedBy == email)
                    .ToList();
            }

            // Keyword filter (case-insensitive)
            return userCollections
                .Where(c => c.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public async Task UpdateCollectionInMemory(Collection editCollection)
        {
            // Update only the modified collection in the in-memory list
            var index = _appState.collections.FindIndex(c => c.Id == editCollection.Id);
            if (index != -1)
            {
                _appState.collections[index] = editCollection;
            }

        }

        public async Task DeleteCollectionFromMemory(string collectionId)
        {
            // 1. Remove from AppState.collections
            _appState.collections.RemoveAll(c => c.Id == collectionId);

            // 2. Remove related memory snippets from loadedSnippets
            _appState.loadedSnippets.RemoveAll(msl => msl.collectionId == collectionId);
        }        
        
        
        public async Task UpdateSnippetInMemory(string selectedCollectionId, Snippet selectedSnippet)
        {
            var memorySnippetList = _appState.loadedSnippets.FirstOrDefault(msl => msl.collectionId == selectedCollectionId);
            if (memorySnippetList != null)
            {
                var existingSnippet = memorySnippetList.snippetList.FirstOrDefault(s => s.Id == selectedSnippet.Id);
                if (existingSnippet != null)
                {
                    // Update the properties directly
                    existingSnippet.Title = selectedSnippet.Title;
                    existingSnippet.Content = selectedSnippet.Content;
                    existingSnippet.HashtagsInput = selectedSnippet.HashtagsInput;
                    existingSnippet.LastModifiedDate = selectedSnippet.LastModifiedDate;
                    existingSnippet.SharedWith = selectedSnippet.SharedWith;
                    existingSnippet.IsFavourite = selectedSnippet.IsFavourite;
                    // Add any other updated fields
                }
                else
                {
                    // If it's a new snippet for this collection
                    memorySnippetList.snippetList.Add(selectedSnippet);
                }
            }
        }

        public async Task AddNewSnippetInMemory(string selectedCollectionIdCreate, Snippet newSnippet, string snippetId)
        {
            // Assign the generated Id to the newSnippet object
            newSnippet.Id = snippetId;
            newSnippet.LastModifiedDate = DateTime.Now;

            // Add the snippet to _appState.loadedSnippets
            var memorySnippetList = _appState.loadedSnippets.FirstOrDefault(msl => msl.collectionId == selectedCollectionIdCreate);
            if (memorySnippetList != null)
            {
                memorySnippetList.snippetList.Add(newSnippet);
            }
            else
            {
                // If no MemorySnippetList exists for the collection, create one
                _appState.loadedSnippets.Add(new MemorySnippetList
                {
                    collectionId = selectedCollectionIdCreate,
                    snippetList = new List<Snippet> { newSnippet }
                });
            }
        }

        public async Task DeleteSnippetFromMemory(string snippetId)
        {
            foreach (var memorySnippetList in _appState.loadedSnippets)
            {
                var toRemove = memorySnippetList.snippetList.FirstOrDefault(s => s.Id == snippetId);
                if (toRemove != null)
                {
                    memorySnippetList.snippetList.Remove(toRemove);
                    break; 
                }
            }
        }

        public class EmailSendingClass
        {
            public string To { get; set; }
            public string Subject { get; set; }
            public string htmlContent { get; set; }
        }

    }

}
