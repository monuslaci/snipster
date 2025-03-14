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
    public class CommonClasses
    {
        public class LoginModel
        {
            [Required(ErrorMessage = "Email is required")]
            [EmailAddress(ErrorMessage = "Invalid email format")]
            public string Email { get; set; }


            [Required(ErrorMessage = "Password is required")]
            public string Password { get; set; }
        }

        public class ResetModel
        {
            [Required(ErrorMessage = "Email is required")]
            [EmailAddress(ErrorMessage = "Invalid email format")]
            public string Email { get; set; }

        }

        public class LoginReturn
        {
            public bool Result { get; set; }

            public string Description { get; set; }
        }

        public class LoginStorageModel
        {
            public string Email { get; set; }
            public DateTime Expiration { get; set; }
        }


    }



}
