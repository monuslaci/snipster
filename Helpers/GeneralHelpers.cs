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

namespace Snipster.Helpers
{
    public class GeneralHelpers
    {
        public class EmailSendingClass
        {
            public string To { get; set; }
            public string Subject { get; set; }
            public string htmlContent { get; set; }
        }

    }

}
