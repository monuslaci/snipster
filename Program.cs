using AspNetCore.Identity.MongoDbCore;
using Microsoft.AspNetCore.Identity;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Net.NetworkInformation;
using Snipster.Data;
using Snipster.Services;
using Snipster.Components;
using static Snipster.Data.DBContext;
using MongoDB.Driver.Core.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.DataProtection;
using AspNetCore.Identity.MongoDbCore.Extensions;
using AspNetCore.Identity.MongoDbCore.Infrastructure;
using AspNetCore.Identity.MongoDbCore.Models;
using MongoDbGenericRepository.Attributes;
using Microsoft.AspNetCore.Components.Authorization;
using Blazored.Toast;
using SendGrid;
using SendGrid.Helpers.Mail;
using static Snipster.Helpers.GeneralHelpers;
using Snipster.Services.AppStates;
using Snipster.Helpers;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);
var googleClientId = Environment.GetEnvironmentVariable("ClientID")
    ?? Environment.GetEnvironmentVariable("GoogleClientId")
    ?? Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");
var googleClientSecret = Environment.GetEnvironmentVariable("ClientSecret")
    ?? Environment.GetEnvironmentVariable("GoogleClientSecret")
    ?? Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET");
var googleAuthConfigured = !string.IsNullOrWhiteSpace(googleClientId)
    && !string.IsNullOrWhiteSpace(googleClientSecret);
const string GoogleExternalCookieScheme = "GoogleExternal";

try
{


    // Add services to the container.
    builder.Services.AddRazorPages();
    builder.Services.AddServerSideBlazor()
        .AddCircuitOptions(options =>
        {
            options.DetailedErrors = builder.Environment.IsDevelopment()
                || builder.Configuration.GetValue<bool>("DetailedErrors");
        });

    var connectionString = Environment.GetEnvironmentVariable("MongoDb"); // Correct connection string key
    var databaseName = Environment.GetEnvironmentVariable("DatabaseName"); // Read the database name from configuration

    //builder.Services.AddSingleton<IMongoDatabase>(sp =>
    //{
    //    var connectionString = Environment.GetEnvironmentVariable("MongoDb"); // Correct connection string key
    //    var databaseName = Environment.GetEnvironmentVariable("DatabaseName"); // Read the database name from configuration
    //    var client = new MongoClient(connectionString); // Create MongoClient with the connection string
    //    return client.GetDatabase(databaseName); // Return the MongoDatabase instance
    //});


    builder.Services.AddScoped<AppState>();
    // Register MongoDbService with IMongoDatabase and IPasswordHasher<Users>
    builder.Services.AddSingleton<MongoDbService>(sp =>
    {
        var connectionString = Environment.GetEnvironmentVariable("MongoDb"); // Correct connection string key
        var databaseName = Environment.GetEnvironmentVariable("DatabaseName"); // Read the database name from configuration
        var passwordHasher = sp.GetRequiredService<IPasswordHasher<Users>>(); // Get the password hasher from DI container
        return new MongoDbService(connectionString, databaseName, passwordHasher); // Create and return the MongoDbService instance
    });

    // Register the PasswordHasher service (if not registered already)
    builder.Services.AddSingleton<IPasswordHasher<Users>, PasswordHasher<Users>>();

    builder.Services.AddSingleton<IPasswordHasher<Users>, PasswordHasher<Users>>(); // Registers the built-in password hasher

    var authenticationBuilder = builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options =>
        {
            options.LoginPath = "/login";  // Specify login page path
            options.LogoutPath = "/logout"; // Specify logout page path
        })
        .AddCookie(GoogleExternalCookieScheme, options =>
        {
            options.Cookie.Name = "Snipster.GoogleExternal";
            options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        });

    if (googleAuthConfigured)
    {
        authenticationBuilder.AddGoogle(options =>
        {
            options.ClientId = googleClientId!;
            options.ClientSecret = googleClientSecret!;
            options.CallbackPath = "/signin-google";
            options.SignInScheme = GoogleExternalCookieScheme;
            options.SaveTokens = false;
            options.Events.OnRedirectToAuthorizationEndpoint = context =>
            {
                var redirectUri = context.RedirectUri;
                var separator = redirectUri.Contains('?') ? "&" : "?";
                context.Response.Redirect($"{redirectUri}{separator}prompt=select_account");
                return Task.CompletedTask;
            };
        });
    }


    //var sendGridApiKey = builder.Configuration["SendGrid:ApiKey"];
    var sendGridApiKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY");


    // Register SendGridClient
    builder.Services.AddSingleton<ISendGridClient>(new SendGridClient(sendGridApiKey));
    builder.Services.AddScoped<EmailService>();


    var client = new MongoClient(connectionString);
    var database = client.GetDatabase(databaseName);
    // Register the MongoDB service
    builder.Services.AddSingleton<IMongoDatabase>(database);


    //builder.Services.AddSingleton<IMongoDbContext>(new MongoDbContext(connectionString, databaseName));
    //builder.Services.AddSingleton<IMongoDbContext>(sp =>
    //    new MongoDbContext(connectionString, databaseName));

    //builder.Services.AddIdentity<Users, IdentityRole>()
    //    .AddMongoDbStores<Users, IdentityRole, string>(new MongoDbContext(connectionString, databaseName)) // Pass the correct context
    //    .AddDefaultTokenProviders();



    var mongoDbIdentityConfig = new MongoDbIdentityConfiguration
    {
        MongoDbSettings = new MongoDbSettings
        {
            ConnectionString = connectionString,
            DatabaseName = databaseName
        },
        IdentityOptionsAction = options =>
        {
            options.Password.RequiredLength = 6;
            options.Password.RequireDigit = false;
            options.Password.RequireUppercase = false;
        }
    };

    builder.Services.ConfigureMongoDbIdentity<Users, ApplicationRole, string>(mongoDbIdentityConfig)
        .AddDefaultTokenProviders();

    builder.Services.AddSingleton<MongoDbGenericRepository.IMongoDbContext>(
        new MongoDbContext(connectionString, databaseName)
    );

    // Now, register Identity using the correct context type
    builder.Services.AddIdentity<Users, ApplicationRole>()
        .AddMongoDbStores<MongoDbGenericRepository.IMongoDbContext>(
            builder.Services.BuildServiceProvider().GetRequiredService<MongoDbGenericRepository.IMongoDbContext>()
        )
        .AddDefaultTokenProviders();

    // Register MongoDbContext as a Singleton
    builder.Services.AddSingleton<IMongoDbContext>(sp =>
    {
        return new MongoDbContext(connectionString, databaseName);
    });


    builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
    builder.Services.AddScoped<CustomAuthenticationStateProvider>();

    builder.Services.AddAuthorizationCore(); // Add authorization service for Blazor
    builder.Services.AddControllersWithViews();



    //builder.Services.AddScoped<Blazored.Toast.Services.IToastService, Blazored.Toast.Services.ToastService>();
    builder.Services.AddScoped<IEmailService, EmailService>();
    builder.Services.AddScoped<IGeneralHelpers, GeneralHelpers>();


    //builder.Services.AddSingleton<IMongoDbService, MongoDbService>();
    builder.Services.AddBlazoredToast();
}
catch (Exception ex)
{
    Console.WriteLine($"Error during startup: {ex.Message}");
}
var app = builder.Build();

// Ensure static web assets work in production (for scoped CSS like Snipster.styles.css)
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication(); // Enable authentication middleware
app.UseAuthorization();  // Enable authorization middleware

app.MapGet("/login-google", async (HttpContext context) =>
{
    if (!googleAuthConfigured)
        return Results.Redirect("/login?googleLogin=not-configured");

    context.Response.Headers["Referrer-Policy"] = "no-referrer";

    var properties = new AuthenticationProperties
    {
        RedirectUri = "/login-google-complete"
    };

    await context.ChallengeAsync(GoogleDefaults.AuthenticationScheme, properties);
    return Results.Empty;
});

app.MapGet("/login-google-complete", async (
    HttpContext context,
    IDataProtectionProvider dataProtectionProvider,
    MongoDbService mongoDbService) =>
{
    var authenticateResult = await context.AuthenticateAsync(GoogleExternalCookieScheme);
    var principal = authenticateResult.Principal ?? context.User;

    if (principal?.Identity?.IsAuthenticated != true)
        return Results.Redirect("/login?googleLogin=failed");

    var email = principal.FindFirstValue(ClaimTypes.Email);
    if (string.IsNullOrWhiteSpace(email))
        return Results.Redirect("/login?googleLogin=email-missing");

    var firstName = principal.FindFirstValue(ClaimTypes.GivenName) ?? "";
    var lastName = principal.FindFirstValue(ClaimTypes.Surname) ?? "";
    var displayName = principal.FindFirstValue(ClaimTypes.Name) ?? "";

    if (string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(lastName) && !string.IsNullOrWhiteSpace(displayName))
    {
        var nameParts = displayName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        firstName = nameParts.ElementAtOrDefault(0) ?? "";
        lastName = nameParts.ElementAtOrDefault(1) ?? "";
    }

    await mongoDbService.CreateOrUpdateGoogleUserAsync(email, firstName, lastName);

    var protector = dataProtectionProvider.CreateProtector("Snipster.GoogleLogin");
    var loginToken = protector.Protect($"{email}|{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}");

    await context.SignOutAsync(GoogleExternalCookieScheme);

    return Results.Redirect($"/external-login-complete?token={Uri.EscapeDataString(loginToken)}");
});

app.MapBlazorHub();
app.MapRazorPages();
app.MapFallbackToPage("/_Host");

app.Run();


