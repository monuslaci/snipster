using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Snipster.Services
{

    public class CustomAuthenticationStateProvider : AuthenticationStateProvider
    {
        private ClaimsPrincipal _currentUser = new(new ClaimsIdentity()); //initializes _currentUser as an unauthenticated user. ClaimsPrincipal represents a user, and ClaimsIdentity() means the user has no claims, so they are not logged in

        public override async Task<AuthenticationState> GetAuthenticationStateAsync() //When Blazor checks if a user is authenticated (e.g., for [Authorize] pages), it calls GetAuthenticationStateAsync()
        {
            return await Task.FromResult(new AuthenticationState(_currentUser)); //Returns the current authentication state. Since _currentUser is unauthenticated by default, this means that initially, the user is not logged in.
        }

        public async Task MarkUserAsAuthenticated(string email) //This method creates a new authenticated user and notifies the app of the authentication change: mark a user as authenticated by creating a ClaimsPrincipal. Since _currentUser starts as unauthenticated, the app treats the user as not logged in until MarkUserAsAuthenticated(email) is called. 
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, email) //I load the email to the Name field in the ClaimsPrincipal
            }, "auth");

            _currentUser = new ClaimsPrincipal(identity);
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser))); //This tells Blazor that the authentication state has changed. Any UI that depends on authentication (like AuthorizeView) will now reflect the authenticated user.
            await Task.CompletedTask;
        }

        public async Task MarkUserAsLoggedOut()
        {
            _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
            await Task.CompletedTask;
        }

        public async Task LogoutAsync()
        {
            var authState = Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
            NotifyAuthenticationStateChanged(authState);
        }
    }
}


