using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Snipster.Services
{

    public class CustomAuthenticationStateProvider : AuthenticationStateProvider
    {
        private ClaimsPrincipal _currentUser = new(new ClaimsIdentity());

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            return await Task.FromResult(new AuthenticationState(_currentUser));
        }

        public async Task MarkUserAsAuthenticated(string email)
        {
            var identity = new ClaimsIdentity(new[]
            {
        new Claim(ClaimTypes.Name, email)
        }, "auth");

            _currentUser = new ClaimsPrincipal(identity);
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
            await Task.CompletedTask;
        }

        public async Task MarkUserAsLoggedOut()
        {
            _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
            await Task.CompletedTask;
        }
    }
}

